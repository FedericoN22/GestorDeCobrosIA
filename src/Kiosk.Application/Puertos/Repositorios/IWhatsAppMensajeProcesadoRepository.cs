namespace Kiosk.Application.Puertos.Repositorios;

public interface IWhatsAppMensajeProcesadoRepository
{
    Task<bool> IntentarRegistrarAsync(Guid comercioId, string messageId, CancellationToken cancellationToken = default);
}
