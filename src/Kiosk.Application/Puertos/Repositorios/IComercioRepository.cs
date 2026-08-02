using Kiosk.Domain.Comercios;

namespace Kiosk.Application.Puertos.Repositorios;

public interface IComercioRepository
{
    Task<Comercio?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExisteAlgunoAsync(CancellationToken cancellationToken = default);
    void Add(Comercio comercio);
}
