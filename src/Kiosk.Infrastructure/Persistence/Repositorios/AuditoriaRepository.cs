using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Auditoria;
using Kiosk.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Infrastructure.Persistence.Repositorios;

public sealed class AuditoriaRepository : IAuditoriaRepository
{
    private readonly KioskDbContext _db;

    public AuditoriaRepository(KioskDbContext db)
    {
        _db = db;
    }

    public void Add(AuditoriaEvento evento)
        => _db.AuditoriaEventos.Add(evento);

    public async Task<IReadOnlyList<AuditoriaEvento>> ObtenerEnRangoAsync(
        Guid comercioId,
        Canal? canal,
        string? actor,
        string? tipo,
        DateTime desde,
        DateTime hastaExclusivo,
        CancellationToken cancellationToken = default)
    {
        var query = _db.AuditoriaEventos.Where(a =>
            a.ComercioId == comercioId
            && a.CreatedAt >= desde
            && a.CreatedAt < hastaExclusivo);

        if (canal.HasValue)
        {
            query = query.Where(a => a.Canal == canal.Value);
        }

        if (!string.IsNullOrWhiteSpace(actor))
        {
            var actorNormalizado = actor.Trim();
            query = query.Where(a => a.Actor == actorNormalizado);
        }

        if (!string.IsNullOrWhiteSpace(tipo))
        {
            var tipoNormalizado = tipo.Trim();
            query = query.Where(a => a.Tipo == tipoNormalizado);
        }

        var lista = await query.OrderByDescending(a => a.CreatedAt).ToListAsync(cancellationToken);
        return lista;
    }
}
