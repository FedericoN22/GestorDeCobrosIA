using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Auditoria;
using Kiosk.Domain.Common;

namespace Kiosk.Application.Tests.TestDoubles;

public sealed class FakeAuditoriaRepository : IAuditoriaRepository
{
    private readonly List<AuditoriaEvento> _eventos = [];

    public IReadOnlyList<AuditoriaEvento> Eventos => _eventos;

    public void Add(AuditoriaEvento evento)
        => _eventos.Add(evento);

    public Task<IReadOnlyList<AuditoriaEvento>> ObtenerEnRangoAsync(
        Guid comercioId,
        Canal? canal,
        string? actor,
        string? tipo,
        DateTime desde,
        DateTime hastaExclusivo,
        CancellationToken cancellationToken = default)
    {
        var filtrados = _eventos
            .Where(e => e.ComercioId == comercioId && e.CreatedAt >= desde && e.CreatedAt < hastaExclusivo);

        if (canal.HasValue)
        {
            filtrados = filtrados.Where(e => e.Canal == canal.Value);
        }

        if (!string.IsNullOrWhiteSpace(actor))
        {
            filtrados = filtrados.Where(e => e.Actor == actor);
        }

        if (!string.IsNullOrWhiteSpace(tipo))
        {
            filtrados = filtrados.Where(e => e.Tipo == tipo);
        }

        return Task.FromResult<IReadOnlyList<AuditoriaEvento>>(filtrados.ToList());
    }
}
