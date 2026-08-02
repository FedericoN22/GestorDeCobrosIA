using Kiosk.Domain.Common;

namespace Kiosk.Domain.Comercios;

public class Comercio
{
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Comercio() { }

    public static Comercio Crear(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new DomainException("COMERCIO_NOMBRE_REQUERIDO", "El nombre del comercio es obligatorio.");
        }

        var ahora = DateTime.UtcNow;
        return new Comercio
        {
            Id = Guid.NewGuid(),
            Nombre = nombre.Trim(),
            CreatedAt = ahora,
            UpdatedAt = ahora
        };
    }

    public void CambiarNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new DomainException("COMERCIO_NOMBRE_REQUERIDO", "El nombre del comercio es obligatorio.");
        }

        Nombre = nombre.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}
