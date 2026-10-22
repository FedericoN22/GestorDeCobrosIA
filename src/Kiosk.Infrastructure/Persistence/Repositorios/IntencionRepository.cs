using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Whatsapp;
using Kiosk.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Infrastructure.Persistence.Repositorios;

public sealed class IntencionRepository : IIntencionRepository
{
    private static readonly EstadoIntencion[] EstadosPendientes =
    [
        EstadoIntencion.RECIBIDA,
        EstadoIntencion.PARSEADA,
        EstadoIntencion.ACLARACION,
        EstadoIntencion.ESPERANDO_CONFIRMACION
    ];

    private readonly KioskDbContext _db;

    public IntencionRepository(KioskDbContext db)
    {
        _db = db;
    }

    public Task<Intencion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.Intenciones.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public Task<Intencion?> GetPendienteAsync(Guid comercioId, Canal canal, string canalUsuarioId, CancellationToken cancellationToken = default)
        => _db.Intenciones
            .Where(i => i.ComercioId == comercioId && i.Canal == canal && i.CanalUsuarioId == canalUsuarioId && EstadosPendientes.Contains(i.Estado))
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Intencion>> GetEjecutandoAsync(
        Guid comercioId,
        Canal canal,
        string canalUsuarioId,
        CancellationToken cancellationToken = default)
        => await _db.Intenciones
            .Where(i => i.ComercioId == comercioId && i.Canal == canal && i.CanalUsuarioId == canalUsuarioId && i.Estado == EstadoIntencion.EJECUTANDO)
            .OrderByDescending(i => i.UpdatedAt)
            .ToListAsync(cancellationToken);

    public async Task<ResultadoReclamo> TryReclamarAsync(
        Guid id,
        Guid comercioId,
        Canal canal,
        string canalUsuarioId,
        CancellationToken cancellationToken = default)
    {
        var ahora = DateTime.UtcNow;

        var filasAfectadas = await _db.Intenciones
            .Where(i => i.Id == id
                && i.ComercioId == comercioId
                && i.Canal == canal
                && i.CanalUsuarioId == canalUsuarioId
                && i.Estado == EstadoIntencion.ESPERANDO_CONFIRMACION)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(i => i.Estado, EstadoIntencion.EJECUTANDO)
                    .SetProperty(i => i.UpdatedAt, ahora),
                cancellationToken);

        if (filasAfectadas == 1)
        {
            return ResultadoReclamo.Reclamado;
        }

        var estadoActual = await _db.Intenciones
            .Where(i => i.Id == id)
            .Select(i => (EstadoIntencion?)i.Estado)
            .FirstOrDefaultAsync(cancellationToken);

        return estadoActual switch
        {
            EstadoIntencion.EJECUTANDO => ResultadoReclamo.EnEjecucion,
            EstadoIntencion.EJECUTADA => ResultadoReclamo.YaAplicada,
            _ => ResultadoReclamo.NoResuelta
        };
    }

    public void Add(Intencion intencion)
        => _db.Intenciones.Add(intencion);
}
