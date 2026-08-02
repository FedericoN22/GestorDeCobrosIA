using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Comercios;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Infrastructure.Persistence.Repositorios;

public sealed class ComercioRepository : IComercioRepository
{
    private readonly KioskDbContext _db;

    public ComercioRepository(KioskDbContext db)
    {
        _db = db;
    }

    public Task<Comercio?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.Comercios.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> ExisteAlgunoAsync(CancellationToken cancellationToken = default)
        => _db.Comercios.AnyAsync(cancellationToken);

    public void Add(Comercio comercio)
        => _db.Comercios.Add(comercio);
}
