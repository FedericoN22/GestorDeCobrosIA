using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Catalogos;

namespace Kiosk.Application.Tests.TestDoubles;

public sealed class FakeCategoriaRepository : ICategoriaRepository
{
    private readonly List<Categoria> _categorias = [];

    public void Seed(Categoria categoria)
    {
        _categorias.Add(categoria);
    }

    public Task<Categoria?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_categorias.FirstOrDefault(c => c.Id == id));

    public Task<IReadOnlyList<Categoria>> GetActivasAsync(Guid comercioId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Categoria>>(
            _categorias.Where(c => c.ComercioId == comercioId && c.Activa).ToList());

    public Task<IReadOnlyList<Categoria>> GetTodasAsync(Guid comercioId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Categoria>>(
            _categorias.Where(c => c.ComercioId == comercioId).ToList());

    public Task<bool> ExisteNombreAsync(Guid comercioId, string nombre, Guid? excluirId = null, CancellationToken cancellationToken = default)
        => Task.FromResult(_categorias.Any(
            c => c.ComercioId == comercioId && c.Nombre == nombre && c.Id != excluirId));

    public void Add(Categoria categoria)
        => _categorias.Add(categoria);
}
