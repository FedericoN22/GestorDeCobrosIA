using Kiosk.Application.Abstractions;
using Kiosk.Application.Auditoria;
using Kiosk.Application.Puertos;
using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Auditoria;
using Kiosk.Domain.Catalogos;
using Kiosk.Domain.Common;
using Kiosk.Domain.Stock;

namespace Kiosk.Application.CasosUso.Stock;

public sealed record EntradaManualCommand(
    Guid ComercioId,
    Guid PresentacionId,
    int Cantidad,
    Guid? UsuarioId,
    string Actor,
    Canal Origen,
    int? PrecioCostoCentavos = null);

public sealed record AjusteStockCommand(
    Guid ComercioId,
    Guid PresentacionId,
    int Cantidad,
    string Motivo,
    Guid? UsuarioId,
    string Actor,
    Canal Origen);

public sealed record ConfigurarStockMinimoCommand(Guid ComercioId, Guid PresentacionId, int? StockMinimo, string Actor, Canal Origen);

public sealed record StockActualResult(Guid PresentacionId, int StockActual, int? StockMinimo, bool StockBajo);

public sealed class ServicioStock
{
    private readonly IProductRepository _productos;
    private readonly IStockLedger _stockLedger;
    private readonly IAuditoriaRepository _auditoria;
    private readonly IUnitOfWork _unitOfWork;

    public ServicioStock(
        IProductRepository productos,
        IStockLedger stockLedger,
        IAuditoriaRepository auditoria,
        IUnitOfWork unitOfWork)
    {
        _productos = productos;
        _stockLedger = stockLedger;
        _auditoria = auditoria;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<StockActualResult>> EntradaManualAsync(
        EntradaManualCommand command,
        CancellationToken cancellationToken = default)
    {
        var presentacion = await GetPresentacionActivaAsync(command.ComercioId, command.PresentacionId, cancellationToken);
        if (presentacion is null)
        {
            return Result<StockActualResult>.Fail(
                new Error("PRESENTACION_NO_ENCONTRADA", "La presentación no existe o está desactivada."));
        }

        if (command.PrecioCostoCentavos.HasValue)
        {
            presentacion.CambiarPrecioCosto(command.PrecioCostoCentavos);
        }

        var movimiento = MovimientoStock.EntradaManual(command.PresentacionId, command.Cantidad, command.UsuarioId, command.Origen);
        _stockLedger.Add(movimiento);
        AuditoriaRegistrador.Registrar(
            _auditoria,
            command.ComercioId,
            command.Origen,
            command.Actor,
            AuditoriaTipos.EntradaManual,
            new { command.PresentacionId, command.Cantidad, command.PrecioCostoCentavos });
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await ActualizarSnapshotAsync(command.PresentacionId, presentacion, cancellationToken);
    }

    public async Task<Result<StockActualResult>> AjusteAsync(
        AjusteStockCommand command,
        CancellationToken cancellationToken = default)
    {
        var presentacion = await GetPresentacionActivaAsync(command.ComercioId, command.PresentacionId, cancellationToken);
        if (presentacion is null)
        {
            return Result<StockActualResult>.Fail(
                new Error("PRESENTACION_NO_ENCONTRADA", "La presentación no existe o está desactivada."));
        }

        var movimiento = MovimientoStock.Ajuste(command.PresentacionId, command.Cantidad, command.Motivo, command.UsuarioId, command.Origen);
        _stockLedger.Add(movimiento);
        AuditoriaRegistrador.Registrar(
            _auditoria,
            command.ComercioId,
            command.Origen,
            command.Actor,
            AuditoriaTipos.AjusteStock,
            new { command.PresentacionId, command.Cantidad, command.Motivo });
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await ActualizarSnapshotAsync(command.PresentacionId, presentacion, cancellationToken);
    }

    public async Task<Result<StockActualResult>> ConfigurarStockMinimoAsync(
        ConfigurarStockMinimoCommand command,
        CancellationToken cancellationToken = default)
    {
        var presentacion = await GetPresentacionActivaAsync(command.ComercioId, command.PresentacionId, cancellationToken);
        if (presentacion is null)
        {
            return Result<StockActualResult>.Fail(
                new Error("PRESENTACION_NO_ENCONTRADA", "La presentación no existe o está desactivada."));
        }

        presentacion.ConfigurarStockMinimo(command.StockMinimo);
        AuditoriaRegistrador.Registrar(
            _auditoria,
            command.ComercioId,
            command.Origen,
            command.Actor,
            AuditoriaTipos.StockMinimoConfigurado,
            new { command.PresentacionId, command.StockMinimo });
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<StockActualResult>.Ok(ToResult(presentacion));
    }

    private async Task<Result<StockActualResult>> ActualizarSnapshotAsync(
        Guid presentacionId,
        Presentacion presentacion,
        CancellationToken cancellationToken)
    {
        var stock = await _stockLedger.CalcularStockAsync(presentacionId, cancellationToken);
        presentacion.ActualizarStock(stock);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<StockActualResult>.Ok(ToResult(presentacion));
    }

    private static StockActualResult ToResult(Presentacion presentacion) => new(
        presentacion.Id,
        presentacion.StockActual,
        presentacion.StockMinimo,
        presentacion.StockMinimo.HasValue && presentacion.StockActual <= presentacion.StockMinimo.Value);

    private async Task<Presentacion?> GetPresentacionActivaAsync(Guid comercioId, Guid presentacionId, CancellationToken cancellationToken)
    {
        var producto = await _productos.GetByPresentacionIdAsync(presentacionId, cancellationToken);
        if (producto is null || producto.ComercioId != comercioId || !producto.Activo)
        {
            return null;
        }

        return producto.Presentaciones.FirstOrDefault(p => p.Id == presentacionId && p.Activa);
    }
}
