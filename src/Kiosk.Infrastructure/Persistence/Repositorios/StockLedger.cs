using Kiosk.Application.Puertos.Repositorios;
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

    public async Task<IReadOnlyList<MovimientoStock>> ObtenerMovimientosAsync(Guid presentacionId, CancellationToken cancellationToken = default)
    {
        var lista = await _db.MovimientosStock
            .Where(m => m.PresentacionId == presentacionId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
        return lista;
    }

    public void Add(MovimientoStock movimiento)
        => _db.MovimientosStock.Add(movimiento);
}
