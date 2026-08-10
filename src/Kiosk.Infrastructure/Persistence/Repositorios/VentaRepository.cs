using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Ventas;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Infrastructure.Persistence.Repositorios;

public sealed class VentaRepository : IVentaRepository
{
    private readonly KioskDbContext _db;

    public VentaRepository(KioskDbContext db)
    {
        _db = db;
    }

    public Task<Venta?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.Ventas
            .Include(v => v.Lineas)
            .Include(v => v.Pagos)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public async Task<int> GetProximoNumeroAsync(Guid comercioId, CancellationToken cancellationToken = default)
    {
        var maximo = await _db.Ventas
            .Where(v => v.ComercioId == comercioId)
            .MaxAsync(v => (int?)v.Numero, cancellationToken);
        return (maximo ?? 0) + 1;
    }

    public async Task<IReadOnlyList<Venta>> ObtenerEnRangoAsync(Guid comercioId, DateTime desde, DateTime hastaExclusivo, CancellationToken cancellationToken = default)
    {
        var lista = await _db.Ventas
            .Include(v => v.Lineas)
            .Include(v => v.Pagos)
            .Where(v => v.ComercioId == comercioId && v.Fecha >= desde && v.Fecha < hastaExclusivo)
            .OrderBy(v => v.Fecha)
            .ToListAsync(cancellationToken);
        return lista;
    }

    public async Task<IReadOnlyList<LineaVenta>> ObtenerLineasEnRangoAsync(Guid comercioId, DateTime desde, DateTime hastaExclusivo, CancellationToken cancellationToken = default)
    {
        var lista = await _db.Ventas
            .Where(v => v.ComercioId == comercioId && v.Fecha >= desde && v.Fecha < hastaExclusivo)
            .SelectMany(v => v.Lineas)
            .ToListAsync(cancellationToken);
        return lista;
    }

    public void Add(Venta venta)
        => _db.Ventas.Add(venta);
}
