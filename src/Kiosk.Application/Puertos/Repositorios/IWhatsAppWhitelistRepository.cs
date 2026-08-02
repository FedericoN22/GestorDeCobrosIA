using Kiosk.Domain.Whatsapp;

namespace Kiosk.Application.Puertos.Repositorios;

public interface IWhatsAppWhitelistRepository
{
    Task<WhatsappWhitelist?> GetAsync(Guid comercioId, string whatsappNumero, CancellationToken cancellationToken = default);
    Task<bool> EstaAutorizadoAsync(Guid comercioId, string whatsappNumero, CancellationToken cancellationToken = default);
    void Add(WhatsappWhitelist whitelist);
}
