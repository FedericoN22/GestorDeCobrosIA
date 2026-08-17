using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Whatsapp;

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

    public Task<Intencion?> GetPendienteAsync(Guid comercioId, string whatsappNumero, CancellationToken cancellationToken = default)
        => Task.FromResult(_intenciones
            .Where(i => i.ComercioId == comercioId && i.WhatsappNumero == whatsappNumero && EstadosPendientes.Contains(i.Estado))
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefault());

    public void Add(Intencion intencion)
        => _intenciones.Add(intencion);
}
