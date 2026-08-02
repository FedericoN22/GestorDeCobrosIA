using Kiosk.Domain.Common;

namespace Kiosk.Domain.Catalogos;

public class Producto
{
    private readonly List<Presentacion> _presentaciones = [];

    public Guid Id { get; private set; }
    public Guid ComercioId { get; private set; }
    public Guid? CategoriaId { get; private set; }
    public string Nombre { get; private set; } = null!;
    public string NombreNormalizado { get; private set; } = null!;
    public bool Activo { get; private set; }

    public IReadOnlyList<Presentacion> Presentaciones => _presentaciones;

    private Producto() { }

    public static Producto Crear(Guid comercioId, Guid? categoriaId, string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new DomainException("PRODUCTO_NOMBRE_REQUERIDO", "El nombre del producto es obligatorio.");
        }

        return new Producto
        {
            Id = Guid.NewGuid(),
            ComercioId = comercioId,
            CategoriaId = categoriaId,
            Nombre = nombre.Trim(),
            NombreNormalizado = Normalizacion.Normalizar(nombre),
            Activo = true
        };
    }

    public void CambiarNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new DomainException("PRODUCTO_NOMBRE_REQUERIDO", "El nombre del producto es obligatorio.");
        }

        Nombre = nombre.Trim();
        NombreNormalizado = Normalizacion.Normalizar(nombre);
    }

    public void CambiarCategoria(Guid? categoriaId)
    {
        CategoriaId = categoriaId;
    }

    public Presentacion AgregarPresentacion(string nombre, int precioVentaCentavos, int? precioCostoCentavos = null, string? codigoBarras = null)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new DomainException("PRESENTACION_NOMBRE_REQUERIDO", "El nombre de la presentación es obligatorio.");
        }

        if (_presentaciones.Any(p => string.Equals(p.Nombre, nombre.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainException("PRESENTACION_DUPLICADA", $"Ya existe la presentación '{nombre}' para el producto '{Nombre}'.");
        }

        if (!string.IsNullOrWhiteSpace(codigoBarras) &&
            _presentaciones.Any(p => p.Activa && string.Equals(p.CodigoBarras, codigoBarras.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainException("CODIGO_BARRAS_DUPLICADO", $"El código de barras '{codigoBarras}' ya está en uso en este producto.");
        }

        var presentacion = Presentacion.Crear(Id, nombre, precioVentaCentavos, precioCostoCentavos, codigoBarras);
        _presentaciones.Add(presentacion);
        return presentacion;
    }

    public void Desactivar()
    {
        Activo = false;
    }
}
