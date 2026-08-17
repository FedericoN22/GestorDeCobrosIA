using Kiosk.Domain.Common;

namespace Kiosk.Domain.Whatsapp;

public class MensajeWhatsAppProcesado
{
    public long Id { get; private set; }
    public Guid ComercioId { get; private set; }
    public string MessageId { get; private set; } = null!;
    public DateTime ProcesadoEn { get; private set; }

    private MensajeWhatsAppProcesado() { }

    public static MensajeWhatsAppProcesado Registrar(Guid comercioId, string messageId)
    {
        if (comercioId == Guid.Empty)
        {
            throw new DomainException("MENSAJE_COMERCIO_REQUERIDO", "El comercio del mensaje es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(messageId))
        {
            throw new DomainException("MENSAJE_ID_REQUERIDO", "El id del mensaje es obligatorio.");
        }

        return new MensajeWhatsAppProcesado
        {
            ComercioId = comercioId,
            MessageId = messageId.Trim(),
            ProcesadoEn = DateTime.UtcNow
        };
    }
}
