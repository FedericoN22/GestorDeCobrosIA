using Kiosk.Domain.Ventas;

namespace Kiosk.Application.Puertos.Repositorios;

public interface ICajaRepository
{
    Task<Caja?> GetActivaAsync(Guid comercioId, CancellationToken cancellationToken = default);
    Task<Caja?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExisteActivaAsync(Guid comercioId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Caja>> ObtenerPorIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Caja>> ObtenerCerradasAsync(Guid comercioId, Guid? usuarioId, DateTime desde, DateTime hastaExclusivo, bool soloDiferencias, CancellationToken cancellationToken = default);
    void Add(Caja caja);
}
