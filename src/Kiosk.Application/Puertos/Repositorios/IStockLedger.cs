using Kiosk.Domain.Common;
using Kiosk.Domain.Stock;

namespace Kiosk.Application.Puertos.Repositorios;

public interface IStockLedger
{
    Task<int> CalcularStockAsync(Guid presentacionId, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, int>> CalcularStockPorIdsAsync(IEnumerable<Guid> presentacionIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MovimientoStock>> ObtenerMovimientosAsync(Guid presentacionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MovimientoStock>> ObtenerEnRangoAsync(
        Guid comercioId,
        Guid? presentacionId,
        TipoMovimiento? tipo,
        Canal? origen,
        Guid? usuarioId,
        DateTime desde,
        DateTime hastaExclusivo,
        CancellationToken cancellationToken = default);
    void Add(MovimientoStock movimiento);
}
