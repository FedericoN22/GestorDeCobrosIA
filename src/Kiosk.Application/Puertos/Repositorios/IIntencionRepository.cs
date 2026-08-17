using Kiosk.Domain.Whatsapp;

namespace Kiosk.Application.Puertos.Repositorios;

public interface IIntencionRepository
{
    Task<Intencion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Intencion?> GetPendienteAsync(Guid comercioId, string whatsappNumero, CancellationToken cancellationToken = default);
    void Add(Intencion intencion);
}
