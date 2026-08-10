namespace Kiosk.Application.Puertos.Integraciones;

public interface ITranscriber
{
    Task<string> TranscribirAsync(Stream audio, string extension, CancellationToken cancellationToken = default);
}
