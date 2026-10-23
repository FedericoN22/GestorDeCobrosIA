using Kiosk.Application.Abstractions;
using Kiosk.Application.CasosUso.Catalogos;
using Kiosk.Application.CasosUso.Stock;
using Kiosk.Application.Intenciones;
using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Common;
using Kiosk.Domain.Stock;

namespace Kiosk.Application.CasosUso.Whatsapp;

public sealed class EjecutorAcciones
{
    private const int MaxLineasListado = 20;
    private const int MaxCantidadIncrementoAbsoluto = 1000;
    private const int MaxPrecioVentaCentavos = 10_000_000 * 100;

    private readonly ServicioStock _stock;
    private readonly ServicioProductos _productos;
    private readonly ResolvedorCatalogos _resolvedor;
    private readonly IStockLedger _stockLedger;
    private readonly IConfiguracionRepository _config;

    public EjecutorAcciones(
        ServicioStock stock,
        ServicioProductos productos,
        ResolvedorCatalogos resolvedor,
        IStockLedger stockLedger,
        IConfiguracionRepository config)
    {
        _stock = stock;
        _productos = productos;
        _resolvedor = resolvedor;
        _stockLedger = stockLedger;
        _config = config;
    }

    public async Task<Result<string>> EjecutarAsync(
        Guid comercioId,
        StructuredCommand comando,
        CoincidenciaPresentacion? objetivo,
        string actor,
        Guid intencionId,
        CancellationToken cancellationToken = default)
    {
        return comando.Accion switch
        {
            AccionIntencion.CONSULTAR_STOCK => await ConsultarStockAsync(comercioId, objetivo!, actor, cancellationToken),
            AccionIntencion.CONSULTAR_PRECIO => await ConsultarPrecioAsync(comercioId, objetivo!, actor, cancellationToken),
            AccionIntencion.LISTAR_PRODUCTOS => await ListarAsync(comercioId, cancellationToken),
            AccionIntencion.AGREGAR_STOCK => await AgregarStockAsync(comercioId, objetivo!, comando.Parametros, actor, intencionId, cancellationToken),
            AccionIntencion.CREAR_PRODUCTO => await CrearProductoAsync(comercioId, comando.Parametros, actor, cancellationToken),
            AccionIntencion.MODIFICAR_PRECIO => await ModificarPrecioAsync(comercioId, objetivo!, comando.Parametros, actor, cancellationToken),
            AccionIntencion.ELIMINAR_PRODUCTO => await EliminarAsync(comercioId, objetivo!, actor, cancellationToken),
            _ => Result<string>.Fail(new Error("ACCION_DESCONOCIDA", "La acción solicitada no está soportada."))
        };
    }

    private async Task<Result<string>> ConsultarStockAsync(Guid comercioId, CoincidenciaPresentacion objetivo, string actor, CancellationToken ct)
    {
        var presentacion = objetivo.Presentacion;
        var stock = await _stockLedger.CalcularStockAsync(presentacion.Id, ct);
        return Result<string>.Ok(RespuestasBot.StockConsultado(
            objetivo.Producto.Nombre,
            presentacion.Nombre,
            stock,
            presentacion.StockMinimo));
    }

    private async Task<Result<string>> ConsultarPrecioAsync(Guid comercioId, CoincidenciaPresentacion objetivo, string actor, CancellationToken ct)
    {
        return Result<string>.Ok(RespuestasBot.PrecioConsultado(
            objetivo.Producto.Nombre,
            objetivo.Presentacion.Nombre,
            objetivo.Presentacion.PrecioVentaCentavos));
    }

    private async Task<Result<string>> ListarAsync(Guid comercioId, CancellationToken ct)
    {
        var productos = await _resolvedor.ListarActivosAsync(comercioId, ct);

        var lineas = new List<string>();
        var hayMas = false;
        foreach (var producto in productos)
        {
            foreach (var presentacion in producto.Presentaciones.Where(p => p.Activa))
            {
                if (lineas.Count >= MaxLineasListado)
                {
                    hayMas = true;
                    break;
                }

                lineas.Add($"• {producto.Nombre} {presentacion.Nombre}: {RespuestasBot.Pesos(presentacion.PrecioVentaCentavos)} ({presentacion.StockActual} unid.)");
            }

            if (hayMas)
            {
                break;
            }
        }

        if (lineas.Count == 0)
        {
            return Result<string>.Ok("Todavía no hay productos cargados en el catálogo.");
        }

        return Result<string>.Ok(RespuestasBot.ProductosListados(string.Join('\n', lineas), hayMas));
    }

    private async Task<Result<string>> AgregarStockAsync(Guid comercioId, CoincidenciaPresentacion objetivo, ParametrosComando p, string actor, Guid intencionId, CancellationToken ct)
    {
        var cantidad = p.Cantidad ?? 0;

        if (p.TipoPrecio == TipoPrecio.COSTO)
        {
            return Result<string>.Fail(new Error(
                "PRECIO_COSTO_NO_PERMITIDO",
                "El chatbot no puede modificar el precio de costo. Use el panel de administración para actualizar costos."));
        }

        var maxPermitido = await ObtenerMaxCantidadPermitidaAsync(comercioId, ct);
        if (!ValidarLimiteCantidad(cantidad, maxPermitido))
        {
            return Result<string>.Fail(new Error(
                "CANTIDAD_EXCEDE_LIMITE",
                $"La cantidad {cantidad} supera el máximo permitido de {maxPermitido} unidades por operación."));
        }

        if (objetivo.Producto.ComercioId != comercioId)
        {
            return Result<string>.Fail(new Error("PRESENTACION_NO_PERTENECE_COMERCIO", "La presentación no pertenece al comercio."));
        }

        var claveIdempotencia = MovimientoStock.GenerarClaveIdempotencia(
            intencionId,
            "AGREGAR_STOCK",
            objetivo.Presentacion.Id,
            cantidad,
            null);

        var resultado = await _stock.EntradaManualAsync(new EntradaManualCommand(
            comercioId,
            objetivo.Presentacion.Id,
            cantidad,
            null,
            actor,
            Canal.WHATSAPP,
            null,
            claveIdempotencia), ct);

        if (!resultado)
        {
            return Result<string>.Fail(resultado.Error!);
        }

        return Result<string>.Ok(RespuestasBot.StockAgregado(
            objetivo.Producto.Nombre,
            objetivo.Presentacion.Nombre,
            cantidad,
            resultado.Value!.StockActual));
    }

    private async Task<int> ObtenerMaxCantidadPermitidaAsync(Guid comercioId, CancellationToken ct)
    {
        var valor = await _config.GetAsync(comercioId, ClavesConfiguracion.BotStockCantidadMaxima, ct);
        if (int.TryParse(valor?.Valor, out var configurado) && configurado > 0)
        {
            return Math.Min(MaxCantidadIncrementoAbsoluto, configurado);
        }
        return MaxCantidadIncrementoAbsoluto;
    }

    private static bool ValidarLimiteCantidad(int cantidad, int maxPermitido)
        => cantidad > 0 && cantidad <= maxPermitido;

    private async Task<Result<string>> CrearProductoAsync(Guid comercioId, ParametrosComando p, string actor, CancellationToken ct)
    {
        var creado = await _productos.CrearProductoAsync(new CrearProductoCommand(
            comercioId,
            null,
            p.Producto!,
            actor,
            Canal.WHATSAPP), ct);

        if (!creado)
        {
            return Result<string>.Fail(creado.Error!);
        }

        if (string.IsNullOrWhiteSpace(p.Presentacion))
        {
            return Result<string>.Ok(RespuestasBot.ProductoCreado(p.Producto!, null, null));
        }

        var precioVenta = p.Precio.HasValue ? Acentavos(p.Precio.Value) : 0;
        if (precioVenta <= 0)
        {
            return Result<string>.Fail(new Error("PRECIO_REQUERIDO", "Para crear una presentación se necesita un precio de venta."));
        }

        if (precioVenta > MaxPrecioVentaCentavos)
        {
            return Result<string>.Fail(new Error(
                "PRECIO_EXCEDE_LIMITE",
                $"El precio {RespuestasBot.Pesos(precioVenta)} supera el máximo permitido de {RespuestasBot.Pesos(MaxPrecioVentaCentavos)}."));
        }

        var presentacion = await _productos.AgregarPresentacionAsync(new AgregarPresentacionCommand(
                     comercioId,
                     creado.Value!.ProductoId,
                     p.Presentacion,
                     precioVenta,
                     null,
                     null,
                     null,
                     actor,
                     Canal.WHATSAPP), ct);

        if (!presentacion)
        {
            return Result<string>.Fail(presentacion.Error!);
        }

        return Result<string>.Ok(RespuestasBot.ProductoCreado(p.Producto!, p.Presentacion, precioVenta));
    }

    private async Task<Result<string>> ModificarPrecioAsync(Guid comercioId, CoincidenciaPresentacion objetivo, ParametrosComando p, string actor, CancellationToken ct)
    {
        var presentacion = objetivo.Presentacion;

        if (objetivo.Producto.ComercioId != comercioId)
        {
            return Result<string>.Fail(new Error("PRESENTACION_NO_PERTENECE_COMERCIO", "La presentación no pertenece al comercio."));
        }

        var precioVentaCentavos = Acentavos(p.Precio!.Value);

        if (precioVentaCentavos > MaxPrecioVentaCentavos)
        {
            return Result<string>.Fail(new Error(
                "PRECIO_EXCEDE_LIMITE",
                $"El precio {RespuestasBot.Pesos(precioVentaCentavos)} supera el máximo permitido de {RespuestasBot.Pesos(MaxPrecioVentaCentavos)}."));
        }

        var resultado = await _productos.EditarPresentacionAsync(new EditarPresentacionCommand(
                     comercioId,
                     presentacion.Id,
                     presentacion.Nombre,
                     precioVentaCentavos,
                     presentacion.PrecioCostoCentavos,
                     presentacion.CodigoBarras,
                     null,
                     actor,
                     Canal.WHATSAPP), ct);

        if (!resultado)
        {
            return Result<string>.Fail(resultado.Error!);
        }

        return Result<string>.Ok(RespuestasBot.PrecioModificado(
            objetivo.Producto.Nombre,
            presentacion.Nombre,
            precioVentaCentavos));
    }

    private async Task<Result<string>> EliminarAsync(Guid comercioId, CoincidenciaPresentacion objetivo, string actor, CancellationToken ct)
    {
        if (objetivo.Producto.ComercioId != comercioId)
        {
            return Result<string>.Fail(new Error("PRESENTACION_NO_PERTENECE_COMERCIO", "La presentación no pertenece al comercio."));
        }

        var resultado = await _productos.DesactivarPresentacionAsync(new DesactivarPresentacionCommand(
            comercioId,
            objetivo.Presentacion.Id,
            actor,
            Canal.WHATSAPP), ct);

        if (!resultado)
        {
            return Result<string>.Fail(resultado.Error!);
        }

        return Result<string>.Ok(RespuestasBot.ProductoEliminado(
            objetivo.Producto.Nombre,
            objetivo.Presentacion.Nombre));
    }

    private static int Acentavos(int pesos) => checked(pesos * 100);
}
