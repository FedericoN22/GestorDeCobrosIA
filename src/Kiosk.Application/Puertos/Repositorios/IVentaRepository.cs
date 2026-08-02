using Kiosk.Domain.Ventas;

namespace Kiosk.Application.Puertos.Repositorios;

public interface IVentaRepository
{
    Task<Venta?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> GetProximoNumeroAsync(Guid comercioId, CancellationToken cancellationToken = default);
    void Add(Venta venta);
}
