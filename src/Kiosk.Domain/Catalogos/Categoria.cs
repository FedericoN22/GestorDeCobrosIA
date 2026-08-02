using Kiosk.Domain.Common;

namespace Kiosk.Domain.Catalogos;

public class Categoria
{
    public Guid Id { get; private set; }
    public Guid ComercioId { get; private set; }
    public string Nombre { get; private set; } = null!;
    public bool Activa { get; private set; }

    private Categoria() { }

    public static Categoria Crear(Guid comercioId, string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new DomainException("CATEGORIA_NOMBRE_REQUERIDO", "El nombre de la categoría es obligatorio.");
        }

        return new Categoria
        {
            Id = Guid.NewGuid(),
            ComercioId = comercioId,
            Nombre = nombre.Trim(),
            Activa = true
        };
    }

    public void CambiarNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new DomainException("CATEGORIA_NOMBRE_REQUERIDO", "El nombre de la categoría es obligatorio.");
        }

        Nombre = nombre.Trim();
    }

    public void Desactivar()
    {
        Activa = false;
    }
}
