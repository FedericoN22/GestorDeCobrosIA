using Kiosk.Domain.Catalogos;

namespace Kiosk.Application.Puertos.Repositorios;

public interface IProductRepository
{
    Task<Producto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Producto?> GetByPresentacionIdAsync(Guid presentacionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Producto>> GetActivosAsync(Guid comercioId, CancellationToken cancellationToken = default);
    Task<bool> ExisteNombreAsync(Guid comercioId, string nombreNormalizado, CancellationToken cancellationToken = default);
    void Add(Producto producto);
}
