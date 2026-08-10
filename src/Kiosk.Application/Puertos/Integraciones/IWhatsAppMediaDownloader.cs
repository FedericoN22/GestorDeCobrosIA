namespace Kiosk.Application.Puertos.Integraciones;

public interface IWhatsAppMediaDownloader
{
    Task<byte[]?> DescargarAsync(string mediaId, CancellationToken cancellationToken = default);
}
