using Kiosk.Domain.Common;

namespace Kiosk.Domain.Ventas;

public class Venta
{
    private readonly List<LineaVenta> _lineas = [];
    private readonly List<Pago> _pagos = [];

    public Guid Id { get; private set; }
    public Guid ComercioId { get; private set; }
    public Guid CajaId { get; private set; }
    public int Numero { get; private set; }
    public int TotalCentavos { get; private set; }
    public DateTime Fecha { get; private set; }
    public bool ClientGenerated { get; private set; }

    public IReadOnlyList<LineaVenta> Lineas => _lineas;
    public IReadOnlyList<Pago> Pagos => _pagos;

    public int TotalPagadoCentavos => _pagos.Sum(p => p.MontoCentavos);

    private Venta() { }

    public static Venta Crear(Guid comercioId, Guid cajaId, int numero, DateTime fecha, bool clientGenerated = false)
    {
        return new Venta
        {
            Id = Guid.NewGuid(),
            ComercioId = comercioId,
            CajaId = cajaId,
            Numero = numero,
            Fecha = fecha,
            ClientGenerated = clientGenerated,
            TotalCentavos = 0
        };
    }

    public LineaVenta AgregarLinea(Guid presentacionId, string productoNombre, string presentacionNombre, int cantidad, int precioUnitarioCentavos)
    {
        var linea = LineaVenta.Crear(Id, presentacionId, productoNombre, presentacionNombre, cantidad, precioUnitarioCentavos);
        _lineas.Add(linea);
        TotalCentavos = _lineas.Sum(l => l.SubtotalCentavos);
        return linea;
    }

    public Pago AgregarPago(MedioPago medio, int montoCentavos)
    {
        var pago = Pago.Crear(Id, medio, montoCentavos);
        _pagos.Add(pago);
        return pago;
    }

    public void ValidarPagosCompletos()
    {
        if (TotalPagadoCentavos < TotalCentavos)
        {
            throw new DomainException("VENTA_PAGOS_INCOMPLETOS", "El total pagado es menor al total de la venta.");
        }
    }
}
