using Kiosk.Domain.Catalogos;

namespace Kiosk.Application.Puertos.Repositorios;

public interface ICategoriaRepository
{
    Task<Categoria?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Categoria>> GetActivasAsync(Guid comercioId, CancellationToken cancellationToken = default);
    void Add(Categoria categoria);
}
