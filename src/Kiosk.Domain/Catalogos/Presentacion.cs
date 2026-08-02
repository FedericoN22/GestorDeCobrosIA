using Kiosk.Domain.Common;

namespace Kiosk.Domain.Catalogos;

public class Presentacion
{
    public Guid Id { get; private set; }
    public Guid ProductoId { get; private set; }
    public string Nombre { get; private set; } = null!;
    public string? CodigoBarras { get; private set; }
    public int PrecioVentaCentavos { get; private set; }
    public int? PrecioCostoCentavos { get; private set; }
    public bool Activa { get; private set; }
    public int StockActual { get; private set; }
    public int? StockMinimo { get; private set; }

    private Presentacion() { }

    public static Presentacion Crear(Guid productoId, string nombre, int precioVentaCentavos, int? precioCostoCentavos = null, string? codigoBarras = null)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new DomainException("PRESENTACION_NOMBRE_REQUERIDO", "El nombre de la presentación es obligatorio.");
        }

        if (precioVentaCentavos <= 0)
        {
            throw new DomainException("PRECIO_VENTA_INVALIDO", "El precio de venta debe ser mayor a cero.");
        }

        if (precioCostoCentavos is < 0)
        {
            throw new DomainException("PRECIO_COSTO_INVALIDO", "El precio de costo no puede ser negativo.");
        }

        if (!string.IsNullOrWhiteSpace(codigoBarras) && codigoBarras.Trim().Length > 32)
        {
            throw new DomainException("CODIGO_BARRAS_LARGO", "El código de barras no puede superar los 32 caracteres.");
        }

        return new Presentacion
        {
            Id = Guid.NewGuid(),
            ProductoId = productoId,
            Nombre = nombre.Trim(),
            CodigoBarras = string.IsNullOrWhiteSpace(codigoBarras) ? null : codigoBarras.Trim(),
            PrecioVentaCentavos = precioVentaCentavos,
            PrecioCostoCentavos = precioCostoCentavos,
            Activa = true,
            StockActual = 0
        };
    }

    public void CambiarPrecioVenta(int precioVentaCentavos)
    {
        if (precioVentaCentavos <= 0)
        {
            throw new DomainException("PRECIO_VENTA_INVALIDO", "El precio de venta debe ser mayor a cero.");
        }

        PrecioVentaCentavos = precioVentaCentavos;
    }

    public void CambiarNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new DomainException("PRESENTACION_NOMBRE_REQUERIDO", "El nombre de la presentación es obligatorio.");
        }

        Nombre = nombre.Trim();
    }

    public void CambiarPrecioCosto(int? precioCostoCentavos)
    {
        if (precioCostoCentavos is < 0)
        {
            throw new DomainException("PRECIO_COSTO_INVALIDO", "El precio de costo no puede ser negativo.");
        }

        PrecioCostoCentavos = precioCostoCentavos;
    }

    public void CambiarCodigoBarras(string? codigoBarras)
    {
        CodigoBarras = string.IsNullOrWhiteSpace(codigoBarras) ? null : codigoBarras.Trim();
    }

    public void ActualizarStock(int stockCalculado)
    {
        if (stockCalculado < 0)
        {
            throw new DomainException("STOCK_NEGATIVO", "El stock de una presentación no puede ser negativo.");
        }

        StockActual = stockCalculado;
    }

    public void ConfigurarStockMinimo(int? stockMinimo)
    {
        if (stockMinimo is < 0)
        {
            throw new DomainException("STOCK_MINIMO_INVALIDO", "El stock mínimo no puede ser negativo.");
        }

        StockMinimo = stockMinimo;
    }

    public void Desactivar()
    {
        Activa = false;
    }
}
