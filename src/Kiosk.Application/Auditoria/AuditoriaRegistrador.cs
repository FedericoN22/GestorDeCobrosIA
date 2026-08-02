using System.Text.Json;
using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Auditoria;
using Kiosk.Domain.Common;

namespace Kiosk.Application.Auditoria;

internal static class AuditoriaRegistrador
{
    public static void Registrar(IAuditoriaRepository auditoria, Guid comercioId, Canal origen, string actor, string tipo, object detalle)
    {
        var evento = AuditoriaEvento.Registrar(
            comercioId,
            origen,
            actor,
            tipo,
            JsonSerializer.Serialize(detalle));
        auditoria.Add(evento);
    }
}
