using Kiosk.Application.Intenciones;

namespace Kiosk.Application.Puertos.Integraciones;

public interface IIaParser
{
    Task<StructuredCommand> ParsearAsync(string textoNormalizado, CancellationToken cancellationToken = default);
}
