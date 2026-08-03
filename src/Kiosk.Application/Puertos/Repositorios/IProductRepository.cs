using Kiosk.Domain.Catalogos;

namespace Kiosk.Application.Puertos.Repositorios;

public interface IProductRepository
{
    Task<Producto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Producto?> GetByPresentacionIdAsync(Guid presentacionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Producto>> GetActivosAsync(Guid comercioId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Producto>> GetTodosAsync(Guid comercioId, CancellationToken cancellationToken = default);
    Task<bool> ExisteNombreAsync(Guid comercioId, string nombreNormalizado, Guid? excluirId = null, CancellationToken cancellationToken = default);
    Task<bool> ExisteCodigoBarrasAsync(Guid comercioId, string codigoBarras, Guid? excluirPresentacionId = null, CancellationToken cancellationToken = default);
    void Add(Producto producto);
    void AddPresentacion(Presentacion presentacion);
}
