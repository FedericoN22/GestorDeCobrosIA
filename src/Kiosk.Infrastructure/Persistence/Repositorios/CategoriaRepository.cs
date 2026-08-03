using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Catalogos;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Infrastructure.Persistence.Repositorios;

public sealed class CategoriaRepository : ICategoriaRepository
{
    private readonly KioskDbContext _db;

    public CategoriaRepository(KioskDbContext db)
    {
        _db = db;
    }

    public Task<Categoria?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.Categorias.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Categoria>> GetActivasAsync(Guid comercioId, CancellationToken cancellationToken = default)
    {
        var lista = await _db.Categorias
            .Where(c => c.ComercioId == comercioId && c.Activa)
            .OrderBy(c => c.Nombre)
            .ToListAsync(cancellationToken);
        return lista;
    }

    public async Task<IReadOnlyList<Categoria>> GetTodasAsync(Guid comercioId, CancellationToken cancellationToken = default)
    {
        var lista = await _db.Categorias
            .Where(c => c.ComercioId == comercioId)
            .OrderBy(c => c.Nombre)
            .ToListAsync(cancellationToken);
        return lista;
    }

    public Task<bool> ExisteNombreAsync(Guid comercioId, string nombre, Guid? excluirId = null, CancellationToken cancellationToken = default)
        => _db.Categorias.AnyAsync(
            c => c.ComercioId == comercioId
                 && c.Nombre == nombre.Trim()
                 && (!excluirId.HasValue || c.Id != excluirId.Value),
            cancellationToken);

    public void Add(Categoria categoria)
        => _db.Categorias.Add(categoria);
}
