namespace Kiosk.Application.Puertos.Integraciones;

public interface IWhatsAppSender
{
    Task EnviarAsync(string whatsappNumero, string texto, CancellationToken cancellationToken = default);
}
