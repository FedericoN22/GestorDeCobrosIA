namespace Kiosk.Application.Puertos.Integraciones;

public interface IStt
{
    Task<string> TranscribirAsync(byte[] audio, string mimeType, CancellationToken cancellationToken = default);
}
