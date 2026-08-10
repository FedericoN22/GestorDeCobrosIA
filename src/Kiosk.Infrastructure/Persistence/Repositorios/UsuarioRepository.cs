using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Infrastructure.Persistence.Repositorios;

public sealed class UsuarioRepository : IUsuarioRepository
{
    private readonly KioskDbContext _db;

    public UsuarioRepository(KioskDbContext db)
    {
        _db = db;
    }

    public Task<Usuario?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        => _db.Usuarios.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

    public Task<Usuario?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, Usuario>> ObtenerPorIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var listaIds = ids.Distinct().ToList();
        var lista = await _db.Usuarios
            .Where(u => listaIds.Contains(u.Id))
            .ToListAsync(cancellationToken);
        return lista.ToDictionary(u => u.Id);
    }

    public void Add(Usuario usuario)
        => _db.Usuarios.Add(usuario);
}
