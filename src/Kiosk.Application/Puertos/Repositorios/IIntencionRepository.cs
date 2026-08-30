using Kiosk.Domain.Whatsapp;
using Kiosk.Domain.Common;

namespace Kiosk.Application.Puertos.Repositorios;

public interface IIntencionRepository
{
    Task<Intencion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Intencion?> GetPendienteAsync(Guid comercioId, Canal canal, string canalUsuarioId, CancellationToken cancellationToken = default);
    void Add(Intencion intencion);
}
