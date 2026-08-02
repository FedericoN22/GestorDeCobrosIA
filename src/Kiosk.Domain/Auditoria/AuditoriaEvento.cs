using Kiosk.Domain.Common;

namespace Kiosk.Domain.Auditoria;

public class AuditoriaEvento
{
    public long Id { get; private set; }
    public Guid ComercioId { get; private set; }
    public Canal Canal { get; private set; }
    public string Actor { get; private set; } = null!;
    public string Tipo { get; private set; } = null!;
    public string? DetalleJson { get; private set; }
    public Guid? IntencionId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private AuditoriaEvento() { }

    public static AuditoriaEvento Registrar(Guid comercioId, Canal canal, string actor, string tipo, string? detalleJson = null, Guid? intencionId = null)
    {
        if (string.IsNullOrWhiteSpace(actor))
        {
            throw new DomainException("AUDITORIA_ACTOR_REQUERIDO", "El actor de la auditoría es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(tipo))
        {
            throw new DomainException("AUDITORIA_TIPO_REQUERIDO", "El tipo de evento es obligatorio.");
        }

        return new AuditoriaEvento
        {
            ComercioId = comercioId,
            Canal = canal,
            Actor = actor.Trim(),
            Tipo = tipo.Trim(),
            DetalleJson = detalleJson,
            IntencionId = intencionId,
            CreatedAt = DateTime.UtcNow
        };
    }
}
