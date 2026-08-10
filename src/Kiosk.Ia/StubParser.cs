using System.Text.RegularExpressions;
using Kiosk.Application.Intenciones;
using Kiosk.Application.Puertos.Integraciones;

namespace Kiosk.Ia;

public sealed class StubParser : IIaParser
{
    private static readonly Regex RxStock = new(
        @"^(?:CUANTO|QUE) STOCK(?: HAY)?(?: DE)? (.+?)\??$",
        RegexOptions.Compiled);

    private static readonly Regex RxPrecio = new(
        @"^(?:CUANTO SALE|CUANTO CUESTA|PRECIO DE|QUE PRECIO TIENE) (.+?)\??$",
        RegexOptions.Compiled);

    private static readonly Regex RxListar = new(
        @"^(?:LISTAR|LISTAR PRODUCTOS|LISTA DE PRODUCTOS|QUE PRODUCTOS HAY)\s*$",
        RegexOptions.Compiled);

    private static readonly Regex RxAgregarConDetalle = new(
        @"^AGREGAR (.+?)(?:,? (?:CANTIDAD|CANT) (\d+))?(?:,? (?:COSTO|PRECIO) \$?(\d+))?\s*$",
        RegexOptions.Compiled);

    private static readonly Regex RxAgregarCantidadPrimero = new(
        @"^AGREGAR (\d+) (.+?)\s*$",
        RegexOptions.Compiled);

    private static readonly Regex RxCambiarPrecio = new(
        @"^(?:CAMBIAR|MODIFICAR|ACTUALIZAR) (?:EL )?PRECIO DE (.+?) A \$?(\d+)\s*$",
        RegexOptions.Compiled);

    private static readonly Regex RxCrear = new(
        @"^CREAR (?:PRODUCTO )?(.+?)(?:,? PRESENTACION(?: DE)? ([^,]+))?(?:,? PRECIO \$?(\d+))?\s*$",
        RegexOptions.Compiled);

    private static readonly Regex RxEliminar = new(
        @"^(?:ELIMINAR|BORRAR|DAR DE BAJA|QUITAR) (.+?)\s*$",
        RegexOptions.Compiled);

    private static readonly string[] VerbosAccion =
    [
        "AGREGAR", "ELIMINAR", "BORRAR", "CREAR", "CAMBIAR PRECIO",
        "MODIFICAR PRECIO", "LISTAR", "CUANTO STOCK", "CUANTO SALE"
    ];

    public Task<ResultadoParseo> ParsearAsync(string textoNormalizado, CancellationToken cancellationToken = default)
    {
        var texto = textoNormalizado.Trim();

        if (EsMultiComando(texto))
        {
            return Task.FromResult(ResultadoParseo.MultiComando("El mensaje contiene más de una instrucción."));
        }

        var resultado = Interpretar(texto);
        return Task.FromResult(resultado);
    }

    private static ResultadoParseo Interpretar(string texto)
    {
        var stock = RxStock.Match(texto);
        if (stock.Success)
        {
            var (producto, presentacion) = Separar(stock.Groups[1].Value);
            return Ok(AccionIntencion.CONSULTAR_STOCK, producto, presentacion);
        }

        var precio = RxPrecio.Match(texto);
        if (precio.Success)
        {
            var (producto, presentacion) = Separar(precio.Groups[1].Value);
            return Ok(AccionIntencion.CONSULTAR_PRECIO, producto, presentacion);
        }

        if (RxListar.IsMatch(texto))
        {
            return ResultadoParseo.Ok(new StructuredCommand(
                1, AccionIntencion.LISTAR_PRODUCTOS, "PRODUCTO",
                new ParametrosComando(null, null, null, null, TipoPrecio.NO_INDICADO, null, texto),
                0.95m, [], [], texto));
        }

        var agregar = RxAgregarConDetalle.Match(texto);
        if (agregar.Success)
        {
            var (producto, presentacion) = Separar(agregar.Groups[1].Value);
            var cantidad = ParseEntero(agregar.Groups[2].Value);
            var costo = ParseEntero(agregar.Groups[3].Value);
            var faltantes = new List<string>();
            if (!cantidad.HasValue)
            {
                faltantes.Add("cantidad");
            }

            return ResultadoParseo.Ok(new StructuredCommand(
                1, AccionIntencion.AGREGAR_STOCK, "PRESENTACION",
                new ParametrosComando(producto, presentacion, cantidad, costo,
                    costo.HasValue ? TipoPrecio.COSTO : TipoPrecio.NO_INDICADO, null, texto),
                0.9m, faltantes, [], texto));
        }

        var agregarPrimero = RxAgregarCantidadPrimero.Match(texto);
        if (agregarPrimero.Success)
        {
            var (producto, presentacion) = Separar(agregarPrimero.Groups[2].Value);
            var cantidad = ParseEntero(agregarPrimero.Groups[1].Value);
            return ResultadoParseo.Ok(new StructuredCommand(
                1, AccionIntencion.AGREGAR_STOCK, "PRESENTACION",
                new ParametrosComando(producto, presentacion, cantidad, null, TipoPrecio.NO_INDICADO, null, texto),
                0.85m, [], [], texto));
        }

        var cambiarPrecio = RxCambiarPrecio.Match(texto);
        if (cambiarPrecio.Success)
        {
            var (producto, presentacion) = Separar(cambiarPrecio.Groups[1].Value);
            var nuevoPrecio = ParseEntero(cambiarPrecio.Groups[2].Value);
            var faltantes = new List<string>();
            if (!nuevoPrecio.HasValue)
            {
                faltantes.Add("precio");
            }

            return ResultadoParseo.Ok(new StructuredCommand(
                1, AccionIntencion.MODIFICAR_PRECIO, "PRESENTACION",
                new ParametrosComando(producto, presentacion, null, nuevoPrecio, TipoPrecio.VENTA, null, texto),
                0.9m, faltantes, [], texto));
        }

        var crear = RxCrear.Match(texto);
        if (crear.Success)
        {
            var producto = crear.Groups[1].Value.Trim();
            var presentacion = string.IsNullOrWhiteSpace(crear.Groups[2].Value) ? null : crear.Groups[2].Value.Trim();
            var precioCrear = ParseEntero(crear.Groups[3].Value);
            var faltantes = new List<string>();
            if (!(presentacion is null) && !precioCrear.HasValue)
            {
                faltantes.Add("precio");
            }

            return ResultadoParseo.Ok(new StructuredCommand(
                1, AccionIntencion.CREAR_PRODUCTO, "PRODUCTO",
                new ParametrosComando(producto, presentacion, null, precioCrear, TipoPrecio.VENTA, null, texto),
                0.9m, faltantes, [], texto));
        }

        var eliminar = RxEliminar.Match(texto);
        if (eliminar.Success)
        {
            var (producto, presentacion) = Separar(eliminar.Groups[1].Value);
            return Ok(AccionIntencion.ELIMINAR_PRODUCTO, producto, presentacion);
        }

        return ResultadoParseo.Fallo("El stub no pudo interpretar el mensaje.");
    }

    private static ResultadoParseo Ok(AccionIntencion accion, string producto, string? presentacion)
    {
        var comando = new StructuredCommand(
            1, accion, presentacion is null ? "PRODUCTO" : "PRESENTACION",
            new ParametrosComando(producto, presentacion, null, null, TipoPrecio.NO_INDICADO, null, producto),
            0.9m, [], [], producto);

        return ResultadoParseo.Ok(comando);
    }

    private static bool EsMultiComando(string texto)
        => VerbosAccion.Count(v => texto.Contains(v, StringComparison.Ordinal)) > 1;

    private static (string producto, string? presentacion) Separar(string texto)
    {
        var partes = texto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (var i = partes.Length - 1; i > 0; i--)
        {
            if (partes[i].Any(char.IsDigit))
            {
                return (string.Join(' ', partes[..i]), string.Join(' ', partes[i..]));
            }
        }

        return (texto, null);
    }

    private static int? ParseEntero(string valor)
        => string.IsNullOrWhiteSpace(valor) ? null : int.Parse(valor);
}
