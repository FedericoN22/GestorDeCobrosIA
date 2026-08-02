using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Whatsapp;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Infrastructure.Persistence.Repositorios;

public sealed class IntencionRepository : IIntencionRepository
{
    private static readonly EstadoIntencion[] EstadosPendientes =
    [
        EstadoIntencion.RECIBIDA,
        EstadoIntencion.PARSEADA,
        EstadoIntencion.ACLARACION,
        EstadoIntencion.ESPERANDO_CONFIRMACION
    ];

    private readonly KioskDbContext _db;

    public IntencionRepository(KioskDbContext db)
    {
        _db = db;
    }

    public Task<Intencion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.Intenciones.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public Task<Intencion?> GetPendienteAsync(string whatsappNumero, CancellationToken cancellationToken = default)
        => _db.Intenciones
            .Where(i => i.WhatsappNumero == whatsappNumero && EstadosPendientes.Contains(i.Estado))
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public void Add(Intencion intencion)
        => _db.Intenciones.Add(intencion);
}
