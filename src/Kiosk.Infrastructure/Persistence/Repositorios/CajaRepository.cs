using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Ventas;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Infrastructure.Persistence.Repositorios;

public sealed class CajaRepository : ICajaRepository
{
    private readonly KioskDbContext _db;

    public CajaRepository(KioskDbContext db)
    {
        _db = db;
    }

    public Task<Caja?> GetActivaAsync(Guid comercioId, CancellationToken cancellationToken = default)
        => _db.Cajas.FirstOrDefaultAsync(
            c => c.ComercioId == comercioId && c.Estado == EstadoCaja.ABIERTA,
            cancellationToken);

    public Task<Caja?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.Cajas.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> ExisteActivaAsync(Guid comercioId, CancellationToken cancellationToken = default)
        => _db.Cajas.AnyAsync(
            c => c.ComercioId == comercioId && c.Estado == EstadoCaja.ABIERTA,
            cancellationToken);

    public void Add(Caja caja)
        => _db.Cajas.Add(caja);
}
