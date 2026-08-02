using Kiosk.Application.Abstractions;
using Kiosk.Application.Puertos;
using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Catalogos;
using Kiosk.Domain.Common;
using Kiosk.Domain.Stock;

namespace Kiosk.Application.CasosUso.Stock;

public sealed record EntradaManualCommand(Guid ComercioId, Guid PresentacionId, int Cantidad, Guid? UsuarioId, Canal Origen);

public sealed record AjusteStockCommand(Guid ComercioId, Guid PresentacionId, int Cantidad, string Motivo, Guid? UsuarioId, Canal Origen);

public sealed record StockActualResult(Guid PresentacionId, int StockActual);

public sealed class ServicioStock
{
    private readonly IProductRepository _productos;
    private readonly IStockLedger _stockLedger;
    private readonly IUnitOfWork _unitOfWork;

    public ServicioStock(IProductRepository productos, IStockLedger stockLedger, IUnitOfWork unitOfWork)
    {
        _productos = productos;
        _stockLedger = stockLedger;
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

        var movimiento = MovimientoStock.EntradaManual(command.PresentacionId, command.Cantidad, command.UsuarioId, command.Origen);
        _stockLedger.Add(movimiento);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var stock = await _stockLedger.CalcularStockAsync(command.PresentacionId, cancellationToken);
        return Result<StockActualResult>.Ok(new StockActualResult(command.PresentacionId, stock));
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
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var stock = await _stockLedger.CalcularStockAsync(command.PresentacionId, cancellationToken);
        return Result<StockActualResult>.Ok(new StockActualResult(command.PresentacionId, stock));
    }

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
