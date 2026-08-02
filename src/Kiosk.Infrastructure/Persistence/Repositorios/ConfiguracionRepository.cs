using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Configuracion;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Infrastructure.Persistence.Repositorios;

public sealed class ConfiguracionRepository : IConfiguracionRepository
{
    private readonly KioskDbContext _db;

    public ConfiguracionRepository(KioskDbContext db)
    {
        _db = db;
    }

    public Task<Configuracion?> GetAsync(Guid comercioId, string clave, CancellationToken cancellationToken = default)
        => _db.Configuraciones.FirstOrDefaultAsync(
            c => c.ComercioId == comercioId && c.Clave == clave,
            cancellationToken);

    public void Add(Configuracion configuracion)
        => _db.Configuraciones.Add(configuracion);
}
