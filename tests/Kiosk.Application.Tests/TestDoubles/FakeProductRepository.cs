using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Catalogos;

namespace Kiosk.Application.Tests.TestDoubles;

public sealed class FakeProductRepository : IProductRepository
{
    private readonly List<Producto> _productos = [];
    private readonly List<Presentacion> _presentaciones = [];

    public IReadOnlyList<Producto> Productos => _productos;

    public void Seed(Producto producto)
    {
        _productos.Add(producto);
    }

    public void Seed(Presentacion presentacion)
    {
        _presentaciones.Add(presentacion);
    }

    public Task<Producto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_productos.FirstOrDefault(p => p.Id == id));

    public Task<Producto?> GetByPresentacionIdAsync(Guid presentacionId, CancellationToken cancellationToken = default)
        => Task.FromResult(_productos.FirstOrDefault(p => p.Presentaciones.Any(pr => pr.Id == presentacionId)));

    public Task<IReadOnlyList<Producto>> GetActivosAsync(Guid comercioId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Producto>>(
            _productos.Where(p => p.ComercioId == comercioId && p.Activo).ToList());

    public Task<IReadOnlyList<Producto>> GetTodosAsync(Guid comercioId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Producto>>(
            _productos.Where(p => p.ComercioId == comercioId).ToList());

    public Task<bool> ExisteNombreAsync(Guid comercioId, string nombreNormalizado, Guid? excluirId = null, CancellationToken cancellationToken = default)
        => Task.FromResult(_productos.Any(
            p => p.ComercioId == comercioId
                 && p.Activo
                 && p.NombreNormalizado == nombreNormalizado
                 && p.Id != excluirId));

    public Task<bool> ExisteCodigoBarrasAsync(Guid comercioId, string codigoBarras, Guid? excluirPresentacionId = null, CancellationToken cancellationToken = default)
        => Task.FromResult(_productos.Any(
            p => p.ComercioId == comercioId
                 && p.Activo
                 && p.Presentaciones.Any(pr => pr.Activa
                     && pr.CodigoBarras == codigoBarras
                     && pr.Id != excluirPresentacionId)));

    public void Add(Producto producto)
        => _productos.Add(producto);

    public void AddPresentacion(Presentacion presentacion)
        => _presentaciones.Add(presentacion);
}
