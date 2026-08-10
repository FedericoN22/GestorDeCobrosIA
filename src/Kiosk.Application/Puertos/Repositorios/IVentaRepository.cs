using Kiosk.Domain.Ventas;

namespace Kiosk.Application.Puertos.Repositorios;

public interface IVentaRepository
{
    Task<Venta?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> GetProximoNumeroAsync(Guid comercioId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Venta>> ObtenerEnRangoAsync(Guid comercioId, DateTime desde, DateTime hastaExclusivo, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LineaVenta>> ObtenerLineasEnRangoAsync(Guid comercioId, DateTime desde, DateTime hastaExclusivo, CancellationToken cancellationToken = default);
    void Add(Venta venta);
}
