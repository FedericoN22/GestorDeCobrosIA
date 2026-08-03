using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Catalogos;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Infrastructure.Persistence.Repositorios;

public sealed class ProductRepository : IProductRepository
{
    private readonly KioskDbContext _db;

    public ProductRepository(KioskDbContext db)
    {
        _db = db;
    }

    public Task<Producto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.Productos
            .Include(p => p.Presentaciones)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Producto?> GetByPresentacionIdAsync(Guid presentacionId, CancellationToken cancellationToken = default)
        => _db.Productos
            .Include(p => p.Presentaciones)
            .FirstOrDefaultAsync(p => p.Presentaciones.Any(pr => pr.Id == presentacionId), cancellationToken);

    public async Task<IReadOnlyList<Producto>> GetActivosAsync(Guid comercioId, CancellationToken cancellationToken = default)
    {
        var lista = await _db.Productos
            .Where(p => p.ComercioId == comercioId && p.Activo)
            .Include(p => p.Presentaciones)
            .OrderBy(p => p.Nombre)
            .ToListAsync(cancellationToken);
        return lista;
    }

    public async Task<IReadOnlyList<Producto>> GetTodosAsync(Guid comercioId, CancellationToken cancellationToken = default)
    {
        var lista = await _db.Productos
            .Where(p => p.ComercioId == comercioId)
            .Include(p => p.Presentaciones)
            .OrderBy(p => p.Nombre)
            .ToListAsync(cancellationToken);
        return lista;
    }

    public Task<bool> ExisteNombreAsync(Guid comercioId, string nombreNormalizado, Guid? excluirId = null, CancellationToken cancellationToken = default)
        => _db.Productos.AnyAsync(
            p => p.ComercioId == comercioId
                 && p.Activo
                 && p.NombreNormalizado == nombreNormalizado
                 && (!excluirId.HasValue || p.Id != excluirId.Value),
            cancellationToken);

    public Task<bool> ExisteCodigoBarrasAsync(Guid comercioId, string codigoBarras, Guid? excluirPresentacionId = null, CancellationToken cancellationToken = default)
        => _db.Productos.AnyAsync(
            p => p.ComercioId == comercioId
                 && p.Activo
                 && p.Presentaciones.Any(pr => pr.Activa
                     && pr.CodigoBarras == codigoBarras
                     && (!excluirPresentacionId.HasValue || pr.Id != excluirPresentacionId.Value)),
            cancellationToken);

    public void Add(Producto producto)
        => _db.Productos.Add(producto);

    public void AddPresentacion(Presentacion presentacion)
        => _db.Presentaciones.Add(presentacion);
}
