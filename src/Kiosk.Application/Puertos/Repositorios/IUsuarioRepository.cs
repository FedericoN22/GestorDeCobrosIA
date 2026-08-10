using Kiosk.Domain.Usuarios;

namespace Kiosk.Application.Puertos.Repositorios;

public interface IUsuarioRepository
{
    Task<Usuario?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<Usuario?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, Usuario>> ObtenerPorIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    void Add(Usuario usuario);
}
