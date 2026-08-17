using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Whatsapp;

namespace Kiosk.Application.Tests.TestDoubles;

public sealed class FakeWhatsAppWhitelistRepository : IWhatsAppWhitelistRepository
{
    private readonly List<WhatsappWhitelist> _whitelist = [];

    public void Autorizar(Guid comercioId, string whatsappNumero)
        => _whitelist.Add(WhatsappWhitelist.Autorizar(comercioId, whatsappNumero));

    public Task<WhatsappWhitelist?> GetAsync(Guid comercioId, string whatsappNumero, CancellationToken cancellationToken = default)
        => Task.FromResult(_whitelist.FirstOrDefault(w => w.ComercioId == comercioId && w.WhatsappNumero == whatsappNumero));

    public Task<bool> EstaAutorizadoAsync(Guid comercioId, string whatsappNumero, CancellationToken cancellationToken = default)
        => Task.FromResult(_whitelist.Any(w => w.ComercioId == comercioId && w.Activo && w.WhatsappNumero == whatsappNumero));

    public Task<Guid?> BuscarComercioActivoAsync(string whatsappNumero, CancellationToken cancellationToken = default)
        => Task.FromResult(_whitelist.FirstOrDefault(w => w.Activo && w.WhatsappNumero == whatsappNumero)?.ComercioId);

    public Task<IReadOnlyList<WhatsappWhitelist>> ListarAsync(Guid comercioId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<WhatsappWhitelist>>(
            _whitelist.Where(w => w.ComercioId == comercioId).OrderByDescending(w => w.Activo).ToList());

    public Task<WhatsappWhitelist?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_whitelist.FirstOrDefault(w => w.Id == id));

    public void Add(WhatsappWhitelist whitelist)
        => _whitelist.Add(whitelist);
}
