using Kiosk.Domain.Stock;

namespace Kiosk.Application.Puertos.Repositorios;

public interface IStockLedger
{
    Task<int> CalcularStockAsync(Guid presentacionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MovimientoStock>> ObtenerMovimientosAsync(Guid presentacionId, CancellationToken cancellationToken = default);
    void Add(MovimientoStock movimiento);
}
