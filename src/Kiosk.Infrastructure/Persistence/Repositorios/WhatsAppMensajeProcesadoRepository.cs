using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Whatsapp;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Infrastructure.Persistence.Repositorios;

public sealed class WhatsAppMensajeProcesadoRepository : IWhatsAppMensajeProcesadoRepository
{
    private readonly KioskDbContext _db;

    public WhatsAppMensajeProcesadoRepository(KioskDbContext db)
    {
        _db = db;
    }

    public async Task<bool> IntentarRegistrarAsync(Guid comercioId, string messageId, CancellationToken cancellationToken = default)
    {
        _db.MensajesWhatsAppProcesados.Add(MensajeWhatsAppProcesado.Registrar(comercioId, messageId));

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }
}
