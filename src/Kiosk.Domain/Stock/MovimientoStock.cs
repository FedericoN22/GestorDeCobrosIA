using Kiosk.Domain.Common;

namespace Kiosk.Domain.Stock;

public enum TipoMovimiento
{
    ENTRADA_MANUAL = 1,
    AJUSTE = 2,
    VENTA = 3,
    DEVOLUCION = 4
}

public class MovimientoStock
{
    public Guid Id { get; private set; }
    public Guid PresentacionId { get; private set; }
    public TipoMovimiento Tipo { get; private set; }
    public int Cantidad { get; private set; }
    public string? Motivo { get; private set; }
    public Guid? VentaId { get; private set; }
    public Guid? UsuarioId { get; private set; }
    public Canal Origen { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private MovimientoStock() { }

    private static MovimientoStock Crear(Guid presentacionId, TipoMovimiento tipo, int cantidad, string? motivo, Guid? ventaId, Guid? usuarioId, Canal origen)
    {
        return new MovimientoStock
        {
            Id = Guid.NewGuid(),
            PresentacionId = presentacionId,
            Tipo = tipo,
            Cantidad = cantidad,
            Motivo = motivo,
            VentaId = ventaId,
            UsuarioId = usuarioId,
            Origen = origen,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static MovimientoStock EntradaManual(Guid presentacionId, int cantidad, Guid? usuarioId, Canal origen)
    {
        if (cantidad <= 0)
        {
            throw new DomainException("STOCK_CANTIDAD_INVALIDA", "La cantidad de entrada debe ser mayor a cero.");
        }

        return Crear(presentacionId, TipoMovimiento.ENTRADA_MANUAL, cantidad, null, null, usuarioId, origen);
    }

    public static MovimientoStock Ajuste(Guid presentacionId, int cantidad, string motivo, Guid? usuarioId, Canal origen)
    {
        if (cantidad == 0)
        {
            throw new DomainException("STOCK_CANTIDAD_INVALIDA", "Un ajuste de stock no puede tener cantidad cero.");
        }

        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new DomainException("STOCK_MOTIVO_REQUERIDO", "Un ajuste de stock requiere un motivo.");
        }

        return Crear(presentacionId, TipoMovimiento.AJUSTE, cantidad, motivo.Trim(), null, usuarioId, origen);
    }

    public static MovimientoStock Venta(Guid presentacionId, int cantidadVendida, Guid ventaId, Canal origen)
    {
        if (cantidadVendida <= 0)
        {
            throw new DomainException("STOCK_CANTIDAD_INVALIDA", "La cantidad vendida debe ser mayor a cero.");
        }

        return Crear(presentacionId, TipoMovimiento.VENTA, -cantidadVendida, null, ventaId, null, origen);
    }

    public static MovimientoStock Devolucion(Guid presentacionId, int cantidadDevuelta, Guid ventaId, Canal origen)
    {
        if (cantidadDevuelta <= 0)
        {
            throw new DomainException("STOCK_CANTIDAD_INVALIDA", "La cantidad devuelta debe ser mayor a cero.");
        }

        return Crear(presentacionId, TipoMovimiento.DEVOLUCION, cantidadDevuelta, null, ventaId, null, origen);
    }
}
