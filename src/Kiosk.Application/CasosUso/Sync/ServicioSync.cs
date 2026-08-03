using System.Text.Json;
using Kiosk.Application.Abstractions;
using Kiosk.Application.Auditoria;
using Kiosk.Application.Puertos;
using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Auditoria;
using Kiosk.Domain.Catalogos;
using Kiosk.Domain.Common;
using Kiosk.Domain.Sync;
using Kiosk.Domain.Stock;
using Kiosk.Domain.Ventas;

namespace Kiosk.Application.CasosUso.Sync;

public static class TiposOperacion
{
    public const string AbrirCaja = "ABRIR_CAJA";
    public const string CerrarCaja = "CERRAR_CAJA";
    public const string Venta = "VENTA";
    public const string AjusteStock = "AJUSTE_STOCK";
}

public sealed record OperacionSyncCommand(Guid OperationId, string Tipo, JsonElement? Payload);

public sealed record ProcesarBatchCommand(
    Guid ComercioId,
    Guid? UsuarioId,
    string Actor,
    IReadOnlyList<OperacionSyncCommand> Operaciones);

public sealed record ResultadoOperacion(
    Guid OperationId,
    bool Ok,
    object? Resultado,
    string? Error,
    string? Message);

public sealed record ProcesarBatchResult(IReadOnlyList<ResultadoOperacion> Resultados);

public sealed record EstadoSyncResult(
    DateTime Cursor,
    IReadOnlyList<Categoria> Categorias,
    IReadOnlyList<Producto> Productos);

public sealed record ConfirmarSyncResult(int Confirmadas);

public sealed class ServicioSync
{
    private readonly IOperacionSyncRepository _syncOps;
    private readonly ICajaRepository _cajas;
    private readonly IVentaRepository _ventas;
    private readonly IProductRepository _productos;
    private readonly ICategoriaRepository _categorias;
    private readonly IStockLedger _stockLedger;
    private readonly IAuditoriaRepository _auditoria;
    private readonly IUnitOfWork _unitOfWork;

    public ServicioSync(
        IOperacionSyncRepository syncOps,
        ICajaRepository cajas,
        IVentaRepository ventas,
        IProductRepository productos,
        ICategoriaRepository categorias,
        IStockLedger stockLedger,
        IAuditoriaRepository auditoria,
        IUnitOfWork unitOfWork)
    {
        _syncOps = syncOps;
        _cajas = cajas;
        _ventas = ventas;
        _productos = productos;
        _categorias = categorias;
        _stockLedger = stockLedger;
        _auditoria = auditoria;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProcesarBatchResult> ProcesarBatchAsync(
        ProcesarBatchCommand command,
        CancellationToken cancellationToken = default)
    {
        var resultados = new List<ResultadoOperacion>(command.Operaciones.Count);

        foreach (var op in command.Operaciones)
        {
            resultados.Add(await ProcesarAsync(command, op, cancellationToken));
        }

        return new ProcesarBatchResult(resultados);
    }

    public async Task<EstadoSyncResult> ObtenerEstadoAsync(
        Guid comercioId,
        DateTime? cursor,
        CancellationToken cancellationToken = default)
    {
        var categorias = await _categorias.GetTodasAsync(comercioId, cancellationToken);
        var productos = await _productos.GetTodosAsync(comercioId, cancellationToken);
        return new EstadoSyncResult(DateTime.UtcNow, categorias, productos);
    }

    public async Task<Result<ConfirmarSyncResult>> ConfirmarAsync(
        Guid comercioId,
        IReadOnlyList<Guid> operationIds,
        CancellationToken cancellationToken = default)
    {
        if (operationIds.Count == 0)
        {
            return Result<ConfirmarSyncResult>.Ok(new ConfirmarSyncResult(0));
        }

        var operaciones = await _syncOps.GetByIdsAsync(comercioId, operationIds, cancellationToken);
        foreach (var operacion in operaciones)
        {
            operacion.Confirmar();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<ConfirmarSyncResult>.Ok(new ConfirmarSyncResult(operaciones.Count));
    }

    private async Task<ResultadoOperacion> ProcesarAsync(
        ProcesarBatchCommand command,
        OperacionSyncCommand op,
        CancellationToken cancellationToken)
    {
        if (op.OperationId == Guid.Empty)
        {
            return Fallo(op.OperationId, "OPERATION_ID_INVALIDO", "El operationId de la operación es obligatorio.");
        }

        var existente = await _syncOps.GetByOperationIdAsync(command.ComercioId, op.OperationId, cancellationToken);
        if (existente is not null)
        {
            return OkRetry(op.OperationId, existente.ResultadoJson);
        }

        if (op.Payload is null)
        {
            return Fallo(op.OperationId, "PAYLOAD_INVALIDO", "La operación no tiene payload.");
        }

        try
        {
            var resultado = await AplicarAsync(command, op, cancellationToken);
            return Ok(op.OperationId, resultado);
        }
        catch (DomainException ex)
        {
            return Fallo(op.OperationId, ex.Code, ex.Message);
        }
        catch (JsonException)
        {
            return Fallo(op.OperationId, "PAYLOAD_INVALIDO", $"El payload de la operación '{op.Tipo}' es inválido.");
        }
        catch (KeyNotFoundException)
        {
            return Fallo(op.OperationId, "PAYLOAD_INVALIDO", $"Faltan campos en el payload de la operación '{op.Tipo}'.");
        }
    }

    private async Task<object> AplicarAsync(
        ProcesarBatchCommand command,
        OperacionSyncCommand op,
        CancellationToken cancellationToken)
    {
        var payload = op.Payload!.Value;

        return op.Tipo switch
        {
            TiposOperacion.AbrirCaja => await AplicarAbrirCajaAsync(command, op.OperationId, payload, cancellationToken),
            TiposOperacion.CerrarCaja => await AplicarCerrarCajaAsync(command, op.OperationId, payload, cancellationToken),
            TiposOperacion.Venta => await AplicarVentaAsync(command, op.OperationId, payload, cancellationToken),
            TiposOperacion.AjusteStock => await AplicarAjusteStockAsync(command, op.OperationId, payload, cancellationToken),
            _ => throw new DomainException("OPERACION_NO_SOPORTADA", $"El tipo de operación '{op.Tipo}' no está soportado.")
        };
    }

    private async Task<object> AplicarAbrirCajaAsync(
        ProcesarBatchCommand command,
        Guid operationId,
        JsonElement payload,
        CancellationToken cancellationToken)
    {
        var cajaId = payload.GetProperty("cajaId").GetGuid();
        var montoInicialCentavos = payload.GetProperty("montoInicialCentavos").GetInt32();
        var fechaApertura = ObtenerFecha(payload, DateTime.UtcNow);

        if (await _cajas.GetByIdAsync(cajaId, cancellationToken) is not null)
        {
            return new { cajaId };
        }

        if (await _cajas.ExisteActivaAsync(command.ComercioId, cancellationToken))
        {
            throw new DomainException("CAJA_YA_ABIERTA", "Ya existe una caja abierta para este comercio.");
        }

        var usuarioId = ObtenerGuid(payload, "usuarioId") ?? command.UsuarioId;
        if (usuarioId is null)
        {
            throw new DomainException("CAJA_USUARIO_REQUERIDO", "La apertura de caja requiere el usuario.");
        }

        var caja = Caja.Abrir(command.ComercioId, usuarioId.Value, montoInicialCentavos, cajaId, fechaApertura);
        _cajas.Add(caja);
        RegistrarOperacion(command, operationId, TiposOperacion.AbrirCaja, new { cajaId }, new
        {
            caja.Id,
            caja.MontoInicialCentavos,
            caja.FechaApertura
        }, AuditoriaTipos.CajaAbierta);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new { cajaId };
    }

    private async Task<object> AplicarCerrarCajaAsync(
        ProcesarBatchCommand command,
        Guid operationId,
        JsonElement payload,
        CancellationToken cancellationToken)
    {
        var cajaId = payload.GetProperty("cajaId").GetGuid();
        var montoEsperadoCentavos = payload.GetProperty("montoEsperadoCentavos").GetInt32();
        var montoDeclaradoCentavos = payload.GetProperty("montoDeclaradoCentavos").GetInt32();
        var fechaCierre = ObtenerFecha(payload, DateTime.UtcNow);

        var caja = await _cajas.GetByIdAsync(cajaId, cancellationToken);
        if (caja is null || caja.ComercioId != command.ComercioId)
        {
            throw new DomainException("CAJA_NO_ENCONTRADA", "La caja a cerrar no existe para este comercio.");
        }

        if (caja.Estado == EstadoCaja.CERRADA)
        {
            return new { cajaId, diferenciaCentavos = caja.DiferenciaCentavos!.Value };
        }

        caja.Cerrar(montoEsperadoCentavos, montoDeclaradoCentavos, fechaCierre);
        RegistrarOperacion(command, operationId, TiposOperacion.CerrarCaja, new { cajaId, diferenciaCentavos = caja.DiferenciaCentavos!.Value }, new
        {
            caja.Id,
            caja.MontoEsperadoCentavos,
            caja.MontoDeclaradoCentavos,
            caja.DiferenciaCentavos
        }, AuditoriaTipos.CajaCerrada);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new { cajaId, diferenciaCentavos = caja.DiferenciaCentavos!.Value };
    }

    private async Task<object> AplicarVentaAsync(
        ProcesarBatchCommand command,
        Guid operationId,
        JsonElement payload,
        CancellationToken cancellationToken)
    {
        var ventaId = payload.GetProperty("ventaId").GetGuid();
        var cajaId = payload.GetProperty("cajaId").GetGuid();
        var numero = payload.GetProperty("numero").GetInt32();
        var fecha = ObtenerFecha(payload, DateTime.UtcNow);
        var clientGenerated = ObtenerBool(payload, "clientGenerated") ?? false;

        if (await _ventas.GetByIdAsync(ventaId, cancellationToken) is not null)
        {
            return new { ventaId, numero };
        }

        var caja = await _cajas.GetByIdAsync(cajaId, cancellationToken);
        if (caja is null || caja.ComercioId != command.ComercioId)
        {
            throw new DomainException("CAJA_NO_ENCONTRADA", "La caja de la venta no existe para este comercio.");
        }

        if (caja.Estado != EstadoCaja.ABIERTA)
        {
            throw new DomainException("CAJA_CERRADA", "La caja de la venta está cerrada.");
        }

        var lineasPayload = payload.GetProperty("lineas").EnumerateArray().ToList();
        if (lineasPayload.Count == 0)
        {
            throw new DomainException("VENTA_SIN_LINEAS", "La venta debe tener al menos una línea.");
        }

        var pagosPayload = payload.GetProperty("pagos").EnumerateArray().ToList();
        if (pagosPayload.Count == 0)
        {
            throw new DomainException("VENTA_SIN_PAGOS", "La venta debe tener al menos un pago.");
        }

        var venta = Venta.Crear(command.ComercioId, cajaId, numero, fecha, clientGenerated, ventaId);
        var presentaciones = new List<Presentacion>();

        foreach (var linea in lineasPayload)
        {
            var presentacionId = linea.GetProperty("presentacionId").GetGuid();
            var cantidad = linea.GetProperty("cantidad").GetInt32();

            var producto = await _productos.GetByPresentacionIdAsync(presentacionId, cancellationToken);
            var presentacion = producto?.Presentaciones.FirstOrDefault(p => p.Id == presentacionId && p.Activa);
            if (producto is null || producto.ComercioId != command.ComercioId || presentacion is null)
            {
                throw new DomainException("PRESENTACION_NO_ENCONTRADA", "Una presentación de la venta no existe o está desactivada.");
            }

            var stockActual = await _stockLedger.CalcularStockAsync(presentacionId, cancellationToken);
            if (stockActual < cantidad)
            {
                throw new DomainException(
                    "STOCK_INSUFICIENTE",
                    $"Stock insuficiente para '{presentacion.Nombre}' (disponible: {stockActual}).");
            }

            var productoNombre = ObtenerString(linea, "productoNombre") ?? producto.Nombre;
            var presentacionNombre = ObtenerString(linea, "presentacionNombre") ?? presentacion.Nombre;
            var precioUnitarioCentavos = ObtenerInt(linea, "precioUnitarioCentavos") ?? presentacion.PrecioVentaCentavos;

            venta.AgregarLinea(presentacionId, productoNombre, presentacionNombre, cantidad, precioUnitarioCentavos);
            presentaciones.Add(presentacion);
        }

        foreach (var pago in pagosPayload)
        {
            var medio = (MedioPago)pago.GetProperty("medio").GetInt32();
            var montoCentavos = pago.GetProperty("montoCentavos").GetInt32();
            venta.AgregarPago(medio, montoCentavos);
        }

        venta.ValidarPagosCompletos();

        var vueltoCentavos = Math.Max(0, venta.TotalPagadoCentavos - venta.TotalCentavos);
        _ventas.Add(venta);
        foreach (var linea in venta.Lineas)
        {
            _stockLedger.Add(MovimientoStock.Venta(linea.PresentacionId, linea.Cantidad, venta.Id, Canal.POS));
        }

        RegistrarOperacion(command, operationId, TiposOperacion.Venta,
            new { ventaId, numero, totalCentavos = venta.TotalCentavos, vueltoCentavos },
            new
            {
                venta.Id,
                venta.Numero,
                venta.TotalCentavos,
                venta.CajaId,
                venta.Fecha,
                Origen = Canal.POS,
                Lineas = venta.Lineas.Select(l => new { l.PresentacionId, l.Cantidad, l.PrecioUnitarioCentavos }),
                Pagos = venta.Pagos.Select(p => new { p.Medio, p.MontoCentavos })
            },
            AuditoriaTipos.VentaRegistrada);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var presentacion in presentaciones.DistinctBy(p => p.Id))
        {
            var stock = await _stockLedger.CalcularStockAsync(presentacion.Id, cancellationToken);
            presentacion.ActualizarStock(stock);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new { ventaId, numero, totalCentavos = venta.TotalCentavos, vueltoCentavos };
    }

    private async Task<object> AplicarAjusteStockAsync(
        ProcesarBatchCommand command,
        Guid operationId,
        JsonElement payload,
        CancellationToken cancellationToken)
    {
        var presentacionId = payload.GetProperty("presentacionId").GetGuid();
        var cantidad = payload.GetProperty("cantidad").GetInt32();
        var motivo = ObtenerString(payload, "motivo");
        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new DomainException("STOCK_MOTIVO_REQUERIDO", "Un ajuste de stock requiere un motivo.");
        }

        var producto = await _productos.GetByPresentacionIdAsync(presentacionId, cancellationToken);
        var presentacion = producto?.Presentaciones.FirstOrDefault(p => p.Id == presentacionId && p.Activa);
        if (producto is null || producto.ComercioId != command.ComercioId || presentacion is null)
        {
            throw new DomainException("PRESENTACION_NO_ENCONTRADA", "La presentación del ajuste no existe o está desactivada.");
        }

        var stockActual = await _stockLedger.CalcularStockAsync(presentacionId, cancellationToken);
        if (stockActual + cantidad < 0)
        {
            throw new DomainException(
                "STOCK_NEGATIVO_PROYECTADO",
                $"El ajuste dejaría stock negativo (disponible: {stockActual}).");
        }

        var movimiento = MovimientoStock.Ajuste(presentacionId, cantidad, motivo, command.UsuarioId, Canal.POS);
        _stockLedger.Add(movimiento);
        RegistrarOperacion(command, operationId, TiposOperacion.AjusteStock,
            new { presentacionId, cantidad },
            new { presentacionId, cantidad, motivo },
            AuditoriaTipos.AjusteStock);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var stock = await _stockLedger.CalcularStockAsync(presentacionId, cancellationToken);
        presentacion.ActualizarStock(stock);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new { presentacionId, cantidad };
    }

    private void RegistrarOperacion(
        ProcesarBatchCommand command,
        Guid operationId,
        string tipo,
        object resultado,
        object detalleAuditoria,
        string tipoAuditoria)
    {
        _syncOps.Add(OperacionSync.Registrar(
            command.ComercioId,
            operationId,
            tipo,
            JsonSerializer.Serialize(resultado)));
        AuditoriaRegistrador.Registrar(
            _auditoria,
            command.ComercioId,
            Canal.POS,
            command.Actor,
            tipoAuditoria,
            detalleAuditoria);
    }

    private static ResultadoOperacion Ok(Guid operationId, object resultado)
        => new(operationId, true, resultado, null, null);

    private static ResultadoOperacion OkRetry(Guid operationId, string? resultadoJson)
    {
        object? resultado = null;
        if (!string.IsNullOrWhiteSpace(resultadoJson))
        {
            resultado = JsonSerializer.Deserialize<JsonElement>(resultadoJson);
        }

        return new ResultadoOperacion(operationId, true, resultado, null, null);
    }

    private static ResultadoOperacion Fallo(Guid operationId, string error, string message)
        => new(operationId, false, null, error, message);

    private static DateTime ObtenerFecha(JsonElement payload, DateTime porDefecto)
        => payload.TryGetProperty("fecha", out var f) && f.TryGetDateTime(out var fecha)
            ? fecha
            : porDefecto;

    private static Guid? ObtenerGuid(JsonElement payload, string nombre)
        => payload.TryGetProperty(nombre, out var p) && p.TryGetGuid(out var valor) ? valor : null;

    private static string? ObtenerString(JsonElement payload, string nombre)
        => payload.TryGetProperty(nombre, out var p) ? p.GetString() : null;

    private static int? ObtenerInt(JsonElement payload, string nombre)
        => payload.TryGetProperty(nombre, out var p) && p.TryGetInt32(out var valor) ? valor : null;

    private static bool? ObtenerBool(JsonElement payload, string nombre)
        => payload.TryGetProperty(nombre, out var p)
            && (p.ValueKind == JsonValueKind.True || p.ValueKind == JsonValueKind.False)
            ? p.GetBoolean()
            : null;
}
