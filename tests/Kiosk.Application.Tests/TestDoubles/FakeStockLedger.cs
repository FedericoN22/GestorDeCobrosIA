using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Common;
using Kiosk.Domain.Stock;

namespace Kiosk.Application.Tests.TestDoubles;

public sealed class FakeStockLedger : IStockLedger
{
    private readonly object _lock = new();
    private readonly List<MovimientoStock> _comprometidos = [];
    private readonly List<MovimientoStock> _pendientes = [];

    public IReadOnlyList<MovimientoStock> Movimientos
    {
        get
        {
            lock (_lock)
            {
                return _comprometidos.ToList();
            }
        }
    }

    public void Seed(MovimientoStock movimiento)
    {
        lock (_lock)
        {
            _comprometidos.Add(movimiento);
        }
    }

    public void Add(MovimientoStock movimiento)
    {
        lock (_lock)
        {
            _pendientes.Add(movimiento);
        }
    }

    public void Commit()
    {
        lock (_lock)
        {
            _comprometidos.AddRange(_pendientes);
            _pendientes.Clear();
        }
    }

    public Task<int> CalcularStockAsync(Guid presentacionId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_comprometidos
                .Where(m => m.PresentacionId == presentacionId)
                .Sum(m => m.Cantidad));
        }
    }

    public Task<Dictionary<Guid, int>> CalcularStockPorIdsAsync(IEnumerable<Guid> presentacionIds, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var ids = presentacionIds.ToHashSet();
            return Task.FromResult(_comprometidos
                .Where(m => ids.Contains(m.PresentacionId))
                .GroupBy(m => m.PresentacionId)
                .ToDictionary(g => g.Key, g => g.Sum(m => m.Cantidad)));
        }
    }

    public Task<IReadOnlyList<MovimientoStock>> ObtenerMovimientosAsync(Guid presentacionId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<MovimientoStock>>(
                _comprometidos.Where(m => m.PresentacionId == presentacionId).OrderBy(m => m.CreatedAt).ToList());
        }
    }

    public Task<IReadOnlyList<MovimientoStock>> ObtenerEnRangoAsync(
        Guid comercioId,
        Guid? presentacionId,
        TipoMovimiento? tipo,
        Canal? origen,
        Guid? usuarioId,
        DateTime desde,
        DateTime hastaExclusivo,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var filtrados = _comprometidos
                .Where(m => m.CreatedAt >= desde && m.CreatedAt < hastaExclusivo);

            if (presentacionId.HasValue)
            {
                filtrados = filtrados.Where(m => m.PresentacionId == presentacionId.Value);
            }

            if (tipo.HasValue)
            {
                filtrados = filtrados.Where(m => m.Tipo == tipo.Value);
            }

            if (origen.HasValue)
            {
                filtrados = filtrados.Where(m => m.Origen == origen.Value);
            }

            if (usuarioId.HasValue)
            {
                filtrados = filtrados.Where(m => m.UsuarioId == usuarioId.Value);
            }

            return Task.FromResult<IReadOnlyList<MovimientoStock>>(filtrados.ToList());
        }
    }
}
