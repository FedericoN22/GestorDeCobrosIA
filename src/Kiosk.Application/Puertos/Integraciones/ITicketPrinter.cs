namespace Kiosk.Application.Puertos.Integraciones;

public interface ITicketPrinter
{
    Task ImprimirAsync(string contenido, CancellationToken cancellationToken = default);
}
