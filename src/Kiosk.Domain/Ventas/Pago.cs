using Kiosk.Domain.Common;

namespace Kiosk.Domain.Ventas;

public enum MedioPago
{
    EFECTIVO = 1,
    TARJETA = 2,
    TRANSFERENCIA_QR = 3
}

public class Pago
{
    public Guid Id { get; private set; }
    public Guid VentaId { get; private set; }
    public MedioPago Medio { get; private set; }
    public int MontoCentavos { get; private set; }

    private Pago() { }

    public static Pago Crear(Guid ventaId, MedioPago medio, int montoCentavos)
    {
        if (montoCentavos <= 0)
        {
            throw new DomainException("PAGO_MONTO_INVALIDO", "El monto del pago debe ser mayor a cero.");
        }

        return new Pago
        {
            Id = Guid.NewGuid(),
            VentaId = ventaId,
            Medio = medio,
            MontoCentavos = montoCentavos
        };
    }
}
