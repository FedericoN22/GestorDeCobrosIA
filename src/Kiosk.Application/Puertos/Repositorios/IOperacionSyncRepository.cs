using Kiosk.Domain.Sync;

namespace Kiosk.Application.Puertos.Repositorios;

public interface IOperacionSyncRepository
{
    Task<OperacionSync?> GetByOperationIdAsync(Guid comercioId, Guid operationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OperacionSync>> GetByIdsAsync(Guid comercioId, IReadOnlyList<Guid> operationIds, CancellationToken cancellationToken = default);
    void Add(OperacionSync operacion);
}
