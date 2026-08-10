namespace Kiosk.Application.Puertos.Integraciones;

public interface IIaParser
{
    Task<ResultadoParseo> ParsearAsync(string textoNormalizado, CancellationToken cancellationToken = default);
}
