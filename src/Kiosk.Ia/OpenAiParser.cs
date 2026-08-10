using System.Net.Http.Json;
using System.Text.Json;
using Kiosk.Application.Intenciones;
using Kiosk.Application.Puertos.Integraciones;

namespace Kiosk.Ia;

public sealed class OpenAiParser : IIaParser
{
    private const string PromptSistema =
        """
        Sos el parser de un asistente de kiosco. Convertís el pedido del administrador en un comando estructurado según el esquema.
        Reglas:
        - Hay UN SOLO comando por mensaje. Si el mensaje tiene más de una instrucción, devolvé "multi_comando": true y explicá el motivo.
        - El precio siempre es un entero en pesos (no centavos).
        - "confianza" es tu certeza de interpretación, de 0 a 1.
        - Si falta información obligatoria para la acción, listala en "campos_faltantes" (ej: "cantidad", "presentacion").
        - Si un campo tiene más de una interpretación posible, listalo en "campos_ambiguos".
        - "tipo_precio": "VENTA" si es precio de venta, "COSTO" si es precio de costo, "NO_INDICADO" si no se aclara.
        - "producto" es el nombre genérico (ej: "Coca Cola"); "presentacion" es tamaño/variante (ej: "2.25L").
        - Si no podés interpretar el mensaje, bajá "confianza" por debajo de 0.7 y explicá en "motivo".
        Ejemplos:
        "¿Cuánto stock hay de coca cola?" -> CONSULTAR_STOCK, producto "coca cola".
        "cuánto sale la quilmes 1L" -> CONSULTAR_PRECIO, producto "quilmes", presentacion "1L".
        "agregar coca cola 2.25L, cantidad 12, precio 4200" -> AGREGAR_STOCK, tipo_precio NO_INDICADO.
        "cambiar precio de coca cola 2.25L a 4500" -> MODIFICAR_PRECIO, tipo_precio VENTA.
        "crear producto pepsi, presentación 1.5L, precio 2500" -> CREAR_PRODUCTO, tipo_precio VENTA.
        "eliminar coca cola 2.25L" -> ELIMINAR_PRODUCTO.
        """;

    private static readonly object FuncionParseo = new
    {
        name = "parsear_comando",
        description = "Convierte un pedido de administrador de kiosco en un comando estructurado. Devuelve exactamente el JSON del esquema.",
        strict = true,
        parameters = new
        {
            type = "object",
            properties = new
            {
                accion = new
                {
                    type = "string",
                    @enum = new[] { "CONSULTAR_STOCK", "CONSULTAR_PRECIO", "LISTAR_PRODUCTOS", "AGREGAR_STOCK", "CREAR_PRODUCTO", "MODIFICAR_PRECIO", "ELIMINAR_PRODUCTO" }
                },
                entidad = new { type = "string", @enum = new[] { "PRODUCTO", "PRESENTACION" } },
                parametros = new
                {
                    type = "object",
                    properties = new
                    {
                        producto = new { type = new[] { "string", "null" } },
                        presentacion = new { type = new[] { "string", "null" } },
                        cantidad = new { type = new[] { "integer", "null" } },
                        precio = new { type = new[] { "integer", "null" } },
                        tipo_precio = new { type = "string", @enum = new[] { "VENTA", "COSTO", "NO_INDICADO" } },
                        categoria = new { type = new[] { "string", "null" } },
                        texto = new { type = new[] { "string", "null" } }
                    },
                    required = new[] { "producto", "presentacion", "cantidad", "precio", "tipo_precio", "categoria", "texto" },
                    additionalProperties = false
                },
                confianza = new { type = "number" },
                campos_faltantes = new { type = "array", items = new { type = "string" } },
                campos_ambiguos = new { type = "array", items = new { type = "string" } },
                texto_original = new { type = "string" },
                multi_comando = new { type = "boolean" },
                motivo = new { type = new[] { "string", "null" } }
            },
            required = new[] { "accion", "entidad", "parametros", "confianza", "campos_faltantes", "campos_ambiguos", "texto_original", "multi_comando", "motivo" },
            additionalProperties = false
        }
    };

    private readonly IHttpClientFactory _http;
    private readonly OpenAiOptions _options;

    public OpenAiParser(IHttpClientFactory http, OpenAiOptions options)
    {
        _http = http;
        _options = options;
    }

    public async Task<ResultadoParseo> ParsearAsync(string textoNormalizado, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            model = _options.Modelo,
            messages = new object[]
            {
                new { role = "system", content = PromptSistema },
                new { role = "user", content = textoNormalizado }
            },
            tools = new object[] { new { type = "function", function = FuncionParseo } },
            tool_choice = "required",
            temperature = 0
        };

        var client = _http.CreateClient(nameof(OpenAiParser));
        using var contenido = JsonContent.Create(request);
        var response = await client.PostAsync("/v1/chat/completions", contenido, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var cuerpo = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"OpenAI devolvió {(int)response.StatusCode}: {cuerpo}");
        }

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var arguments = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("tool_calls")[0]
            .GetProperty("function")
            .GetProperty("arguments")
            .GetString();

        return Mapear(arguments ?? "{}", textoNormalizado);
    }

    private static ResultadoParseo Mapear(string arguments, string textoOriginal)
    {
        using var doc = JsonDocument.Parse(arguments);
        var root = doc.RootElement;

        if (root.TryGetProperty("multi_comando", out var multi) && multi.ValueKind == JsonValueKind.True)
        {
            return ResultadoParseo.MultiComando(
                root.TryGetProperty("motivo", out var m) ? m.GetString() ?? "Más de un comando en el mensaje." : "Más de un comando en el mensaje.");
        }

        if (!root.TryGetProperty("accion", out var accionEl))
        {
            return ResultadoParseo.Fallo("Faltó la acción en la respuesta del modelo.");
        }

        var accion = Enum.Parse<AccionIntencion>(accionEl.GetString()!, ignoreCase: true);
        var entidad = root.TryGetProperty("entidad", out var entidadEl) ? entidadEl.GetString() : "PRODUCTO";
        var confianza = root.TryGetProperty("confianza", out var confianzaEl) ? confianzaEl.GetDecimal() : 0m;

        var parametrosEl = root.TryGetProperty("parametros", out var p) ? p : default;
        var tipoPrecio = Enum.Parse<TipoPrecio>(
            GetString(parametrosEl, "tipo_precio") ?? "NO_INDICADO",
            ignoreCase: true);

        var parametros = new ParametrosComando(
            GetString(parametrosEl, "producto"),
            GetString(parametrosEl, "presentacion"),
            GetInt(parametrosEl, "cantidad"),
            GetInt(parametrosEl, "precio"),
            tipoPrecio,
            GetString(parametrosEl, "categoria"),
            GetString(parametrosEl, "texto"));

        var comando = new StructuredCommand(
            1,
            accion,
            entidad ?? "PRODUCTO",
            parametros,
            confianza,
            LeerArray(root, "campos_faltantes"),
            LeerArray(root, "campos_ambiguos"),
            textoOriginal);

        return ResultadoParseo.Ok(comando);
    }

    private static IReadOnlyList<string> LeerArray(JsonElement element, string propiedad)
    {
        if (!element.TryGetProperty(propiedad, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var resultado = new List<string>();
        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                resultado.Add(item.GetString()!);
            }
        }

        return resultado;
    }

    private static string? GetString(JsonElement element, string propiedad)
    {
        if (element.ValueKind == JsonValueKind.Undefined || !element.TryGetProperty(propiedad, out var value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private static int? GetInt(JsonElement element, string propiedad)
    {
        if (element.ValueKind == JsonValueKind.Undefined || !element.TryGetProperty(propiedad, out var value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var n) ? n : null;
    }
}
