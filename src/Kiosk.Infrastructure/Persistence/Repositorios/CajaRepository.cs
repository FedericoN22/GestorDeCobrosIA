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

    public async Task<IReadOnlyList<Caja>> ObtenerPorIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var listaIds = ids.Distinct().ToList();
        var lista = await _db.Cajas
            .Where(c => listaIds.Contains(c.Id))
            .ToListAsync(cancellationToken);
        return lista;
    }

    public async Task<IReadOnlyList<Caja>> ObtenerCerradasAsync(Guid comercioId, Guid? usuarioId, DateTime desde, DateTime hastaExclusivo, bool soloDiferencias, CancellationToken cancellationToken = default)
    {
        var query = _db.Cajas.Where(c =>
            c.ComercioId == comercioId
            && c.Estado == EstadoCaja.CERRADA
            && c.FechaCierre >= desde
            && c.FechaCierre < hastaExclusivo);

        if (usuarioId.HasValue)
        {
            query = query.Where(c => c.UsuarioId == usuarioId.Value);
        }

        if (soloDiferencias)
        {
            query = query.Where(c => c.DiferenciaCentavos != 0);
        }

        var lista = await query.OrderBy(c => c.FechaApertura).ToListAsync(cancellationToken);
        return lista;
    }

    public void Add(Caja caja)
        => _db.Cajas.Add(caja);
}
