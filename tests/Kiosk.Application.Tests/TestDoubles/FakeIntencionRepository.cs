using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Whatsapp;
using Kiosk.Domain.Common;

namespace Kiosk.Application.Tests.TestDoubles;

public sealed class FakeIntencionRepository : IIntencionRepository
{
    private static readonly EstadoIntencion[] EstadosPendientes =
    [
        EstadoIntencion.RECIBIDA,
        EstadoIntencion.PARSEADA,
        EstadoIntencion.ACLARACION,
        EstadoIntencion.ESPERANDO_CONFIRMACION
    ];

    private readonly List<Intencion> _intenciones = [];

    public IReadOnlyList<Intencion> Intenciones => _intenciones;

    public void Seed(Intencion intencion)
        => _intenciones.Add(intencion);

    public Task<Intencion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_intenciones.FirstOrDefault(i => i.Id == id));

    public Task<Intencion?> GetPendienteAsync(Guid comercioId, Canal canal, string canalUsuarioId, CancellationToken cancellationToken = default)
        => Task.FromResult(_intenciones
            .Where(i => i.ComercioId == comercioId && i.Canal == canal && i.CanalUsuarioId == canalUsuarioId && EstadosPendientes.Contains(i.Estado))
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefault());

    public Task<IReadOnlyList<Intencion>> GetEjecutandoAsync(
        Guid comercioId,
        Canal canal,
        string canalUsuarioId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Intencion>>(_intenciones
            .Where(i => i.ComercioId == comercioId && i.Canal == canal && i.CanalUsuarioId == canalUsuarioId && i.Estado == EstadoIntencion.EJECUTANDO)
            .OrderByDescending(i => i.UpdatedAt)
            .ToList());

    public Task<ResultadoReclamo> TryReclamarAsync(
        Guid id,
        Guid comercioId,
        Canal canal,
        string canalUsuarioId,
        CancellationToken cancellationToken = default)
    {
        var intencion = _intenciones.FirstOrDefault(i => i.Id == id);

        if (intencion is null
            || intencion.ComercioId != comercioId
            || intencion.Canal != canal
            || intencion.CanalUsuarioId != canalUsuarioId)
        {
            return Task.FromResult(ResultadoReclamo.NoResuelta);
        }

        if (intencion.Estado == EstadoIntencion.ESPERANDO_CONFIRMACION)
        {
            intencion.MarcarEjecutando();
            return Task.FromResult(ResultadoReclamo.Reclamado);
        }

        return Task.FromResult(intencion.Estado switch
        {
            EstadoIntencion.EJECUTANDO => ResultadoReclamo.EnEjecucion,
            EstadoIntencion.EJECUTADA => ResultadoReclamo.YaAplicada,
            _ => ResultadoReclamo.NoResuelta
        });
    }

    public void Add(Intencion intencion)
        => _intenciones.Add(intencion);
}
