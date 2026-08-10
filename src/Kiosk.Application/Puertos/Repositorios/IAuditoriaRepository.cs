using Kiosk.Domain.Auditoria;
using Kiosk.Domain.Common;

namespace Kiosk.Application.Puertos.Repositorios;

public interface IAuditoriaRepository
{
    void Add(AuditoriaEvento evento);
    Task<IReadOnlyList<AuditoriaEvento>> ObtenerEnRangoAsync(
        Guid comercioId,
        Canal? canal,
        string? actor,
        string? tipo,
        DateTime desde,
        DateTime hastaExclusivo,
        CancellationToken cancellationToken = default);
}
