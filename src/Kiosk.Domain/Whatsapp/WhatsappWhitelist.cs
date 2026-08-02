using Kiosk.Domain.Common;

namespace Kiosk.Domain.Whatsapp;

public class WhatsappWhitelist
{
    public Guid Id { get; private set; }
    public Guid ComercioId { get; private set; }
    public string WhatsappNumero { get; private set; } = null!;
    public bool Activo { get; private set; }

    private WhatsappWhitelist() { }

    public static WhatsappWhitelist Autorizar(Guid comercioId, string whatsappNumero)
    {
        if (string.IsNullOrWhiteSpace(whatsappNumero))
        {
            throw new DomainException("WHITELIST_NUMERO_REQUERIDO", "El número de WhatsApp es obligatorio.");
        }

        return new WhatsappWhitelist
        {
            Id = Guid.NewGuid(),
            ComercioId = comercioId,
            WhatsappNumero = whatsappNumero.Trim(),
            Activo = true
        };
    }

    public void Desactivar()
    {
        Activo = false;
    }
}
