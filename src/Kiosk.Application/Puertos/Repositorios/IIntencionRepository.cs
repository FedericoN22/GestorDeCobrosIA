using Kiosk.Domain.Whatsapp;
using Kiosk.Domain.Common;

namespace Kiosk.Application.Puertos.Repositorios;

public enum ResultadoReclamo
{
    Reclamado = 1,
    EnEjecucion = 2,
    YaAplicada = 3,
    NoResuelta = 4
}

public interface IIntencionRepository
{
    Task<Intencion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Intencion?> GetPendienteAsync(Guid comercioId, Canal canal, string canalUsuarioId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Intencion>> GetEjecutandoAsync(Guid comercioId, Canal canal, string canalUsuarioId, CancellationToken cancellationToken = default);
    Task<ResultadoReclamo> TryReclamarAsync(Guid id, Guid comercioId, Canal canal, string canalUsuarioId, CancellationToken cancellationToken = default);
    void Add(Intencion intencion);
}
