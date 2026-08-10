using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Common;
using Kiosk.Domain.Stock;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Infrastructure.Persistence.Repositorios;

public sealed class StockLedger : IStockLedger
{
    private readonly KioskDbContext _db;

    public StockLedger(KioskDbContext db)
    {
        _db = db;
    }

    public async Task<int> CalcularStockAsync(Guid presentacionId, CancellationToken cancellationToken = default)
    {
        var suma = await _db.MovimientosStock
            .Where(m => m.PresentacionId == presentacionId)
            .SumAsync(m => (int?)m.Cantidad, cancellationToken);
        return suma ?? 0;
    }

    public async Task<Dictionary<Guid, int>> CalcularStockPorIdsAsync(IEnumerable<Guid> presentacionIds, CancellationToken cancellationToken = default)
    {
        var ids = presentacionIds.Distinct().ToList();
        var agrupado = await _db.MovimientosStock
            .Where(m => ids.Contains(m.PresentacionId))
            .GroupBy(m => m.PresentacionId)
            .Select(g => new { PresentacionId = g.Key, Total = g.Sum(m => (int?)m.Cantidad) })
            .ToListAsync(cancellationToken);

        var resultado = new Dictionary<Guid, int>();
        foreach (var item in agrupado)
        {
            resultado[item.PresentacionId] = item.Total ?? 0;
        }

        return resultado;
    }

    public async Task<IReadOnlyList<MovimientoStock>> ObtenerMovimientosAsync(Guid presentacionId, CancellationToken cancellationToken = default)
    {
        var lista = await _db.MovimientosStock
            .Where(m => m.PresentacionId == presentacionId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
        return lista;
    }

    public async Task<IReadOnlyList<MovimientoStock>> ObtenerEnRangoAsync(
        Guid comercioId,
        Guid? presentacionId,
        TipoMovimiento? tipo,
        Canal? origen,
        Guid? usuarioId,
        DateTime desde,
        DateTime hastaExclusivo,
        CancellationToken cancellationToken = default)
    {
        var presentacionIds = await _db.Presentaciones
            .Where(pr => _db.Productos.Any(p => p.ComercioId == comercioId && p.Id == pr.ProductoId))
            .Select(pr => pr.Id)
            .ToListAsync(cancellationToken);

        var query = _db.MovimientosStock
            .Where(m => presentacionIds.Contains(m.PresentacionId))
            .Where(m => m.CreatedAt >= desde && m.CreatedAt < hastaExclusivo);

        if (presentacionId.HasValue)
        {
            query = query.Where(m => m.PresentacionId == presentacionId.Value);
        }

        if (tipo.HasValue)
        {
            query = query.Where(m => m.Tipo == tipo.Value);
        }

        if (origen.HasValue)
        {
            query = query.Where(m => m.Origen == origen.Value);
        }

        if (usuarioId.HasValue)
        {
            query = query.Where(m => m.UsuarioId == usuarioId.Value);
        }

        var lista = await query.OrderByDescending(m => m.CreatedAt).ToListAsync(cancellationToken);
        return lista;
    }

    public void Add(MovimientoStock movimiento)
        => _db.MovimientosStock.Add(movimiento);
}
