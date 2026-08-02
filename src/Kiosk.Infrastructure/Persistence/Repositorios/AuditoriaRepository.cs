using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Auditoria;

namespace Kiosk.Infrastructure.Persistence.Repositorios;

public sealed class AuditoriaRepository : IAuditoriaRepository
{
    private readonly KioskDbContext _db;

    public AuditoriaRepository(KioskDbContext db)
    {
        _db = db;
    }

    public void Add(AuditoriaEvento evento)
        => _db.AuditoriaEventos.Add(evento);
}
