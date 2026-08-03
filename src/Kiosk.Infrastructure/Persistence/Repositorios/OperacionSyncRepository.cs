using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Sync;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Infrastructure.Persistence.Repositorios;

public sealed class OperacionSyncRepository : IOperacionSyncRepository
{
    private readonly KioskDbContext _db;

    public OperacionSyncRepository(KioskDbContext db)
    {
        _db = db;
    }

    public Task<OperacionSync?> GetByOperationIdAsync(Guid comercioId, Guid operationId, CancellationToken cancellationToken = default)
        => _db.OperacionesSync.FirstOrDefaultAsync(
            o => o.ComercioId == comercioId && o.OperationId == operationId, cancellationToken);

    public async Task<IReadOnlyList<OperacionSync>> GetByIdsAsync(Guid comercioId, IReadOnlyList<Guid> operationIds, CancellationToken cancellationToken = default)
    {
        var lista = await _db.OperacionesSync
            .Where(o => o.ComercioId == comercioId && operationIds.Contains(o.OperationId))
            .ToListAsync(cancellationToken);
        return lista;
    }

    public void Add(OperacionSync operacion)
        => _db.OperacionesSync.Add(operacion);
}
