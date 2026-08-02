using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Whatsapp;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Infrastructure.Persistence.Repositorios;

public sealed class WhatsAppWhitelistRepository : IWhatsAppWhitelistRepository
{
    private readonly KioskDbContext _db;

    public WhatsAppWhitelistRepository(KioskDbContext db)
    {
        _db = db;
    }

    public Task<WhatsappWhitelist?> GetAsync(Guid comercioId, string whatsappNumero, CancellationToken cancellationToken = default)
        => _db.WhatsappWhitelist.FirstOrDefaultAsync(
            w => w.ComercioId == comercioId && w.WhatsappNumero == whatsappNumero,
            cancellationToken);

    public Task<bool> EstaAutorizadoAsync(Guid comercioId, string whatsappNumero, CancellationToken cancellationToken = default)
        => _db.WhatsappWhitelist.AnyAsync(
            w => w.ComercioId == comercioId && w.Activo && w.WhatsappNumero == whatsappNumero,
            cancellationToken);

    public void Add(WhatsappWhitelist whitelist)
        => _db.WhatsappWhitelist.Add(whitelist);
}
