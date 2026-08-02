using Kiosk.Domain.Ventas;

namespace Kiosk.Application.Puertos.Repositorios;

public interface ICajaRepository
{
    Task<Caja?> GetActivaAsync(Guid comercioId, CancellationToken cancellationToken = default);
    Task<Caja?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExisteActivaAsync(Guid comercioId, CancellationToken cancellationToken = default);
    void Add(Caja caja);
}
