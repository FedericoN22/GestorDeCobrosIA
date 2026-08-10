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

    public Task<Guid?> BuscarComercioActivoAsync(string whatsappNumero, CancellationToken cancellationToken = default)
        => _db.WhatsappWhitelist
            .Where(w => w.Activo && w.WhatsappNumero == whatsappNumero)
            .Select(w => (Guid?)w.ComercioId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<WhatsappWhitelist>> ListarAsync(Guid comercioId, CancellationToken cancellationToken = default)
    {
        var lista = await _db.WhatsappWhitelist
            .Where(w => w.ComercioId == comercioId)
            .OrderByDescending(w => w.Activo)
            .ThenBy(w => w.WhatsappNumero)
            .ToListAsync(cancellationToken);
        return lista;
    }

    public Task<WhatsappWhitelist?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.WhatsappWhitelist.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public void Add(WhatsappWhitelist whitelist)
        => _db.WhatsappWhitelist.Add(whitelist);
}
