using Kiosk.Domain.Common;

namespace Kiosk.Domain.Ventas;

public class LineaVenta
{
    public Guid Id { get; private set; }
    public Guid VentaId { get; private set; }
    public Guid PresentacionId { get; private set; }
    public string ProductoNombre { get; private set; } = null!;
    public string PresentacionNombre { get; private set; } = null!;
    public int Cantidad { get; private set; }
    public int PrecioUnitarioCentavos { get; private set; }

    public int SubtotalCentavos => Cantidad * PrecioUnitarioCentavos;

    private LineaVenta() { }

    public static LineaVenta Crear(Guid ventaId, Guid presentacionId, string productoNombre, string presentacionNombre, int cantidad, int precioUnitarioCentavos)
    {
        if (cantidad <= 0)
        {
            throw new DomainException("VENTA_CANTIDAD_INVALIDA", "La cantidad de una línea de venta debe ser mayor a cero.");
        }

        if (precioUnitarioCentavos <= 0)
        {
            throw new DomainException("VENTA_PRECIO_INVALIDO", "El precio unitario de una línea debe ser mayor a cero.");
        }

        if (string.IsNullOrWhiteSpace(productoNombre) || string.IsNullOrWhiteSpace(presentacionNombre))
        {
            throw new DomainException("VENTA_SNAPSHOT_INVALIDO", "Los nombres de producto y presentación son obligatorios.");
        }

        return new LineaVenta
        {
            Id = Guid.NewGuid(),
            VentaId = ventaId,
            PresentacionId = presentacionId,
            ProductoNombre = productoNombre.Trim(),
            PresentacionNombre = presentacionNombre.Trim(),
            Cantidad = cantidad,
            PrecioUnitarioCentavos = precioUnitarioCentavos
        };
    }
}
