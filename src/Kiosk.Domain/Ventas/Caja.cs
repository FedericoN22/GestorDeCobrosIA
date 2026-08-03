using Kiosk.Domain.Common;

namespace Kiosk.Domain.Ventas;

public enum EstadoCaja
{
    ABIERTA = 1,
    CERRADA = 2
}

public class Caja
{
    public Guid Id { get; private set; }
    public Guid ComercioId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public DateTime FechaApertura { get; private set; }
    public int MontoInicialCentavos { get; private set; }
    public DateTime? FechaCierre { get; private set; }
    public int? MontoEsperadoCentavos { get; private set; }
    public int? MontoDeclaradoCentavos { get; private set; }
    public int? DiferenciaCentavos { get; private set; }
    public EstadoCaja Estado { get; private set; }

    private Caja() { }

    public static Caja Abrir(Guid comercioId, Guid usuarioId, int montoInicialCentavos, Guid? id = null, DateTime? fechaApertura = null)
    {
        if (montoInicialCentavos < 0)
        {
            throw new DomainException("CAJA_MONTO_INICIAL_INVALIDO", "El monto inicial no puede ser negativo.");
        }

        return new Caja
        {
            Id = id ?? Guid.NewGuid(),
            ComercioId = comercioId,
            UsuarioId = usuarioId,
            FechaApertura = fechaApertura ?? DateTime.UtcNow,
            MontoInicialCentavos = montoInicialCentavos,
            Estado = EstadoCaja.ABIERTA
        };
    }

    public void Cerrar(int montoEsperadoCentavos, int montoDeclaradoCentavos, DateTime? fechaCierre = null)
    {
        if (Estado == EstadoCaja.CERRADA)
        {
            throw new DomainException("CAJA_YA_CERRADA", "La caja ya está cerrada.");
        }

        if (montoEsperadoCentavos < 0 || montoDeclaradoCentavos < 0)
        {
            throw new DomainException("CAJA_MONTOS_INVALIDOS", "Los montos del cierre no pueden ser negativos.");
        }

        MontoEsperadoCentavos = montoEsperadoCentavos;
        MontoDeclaradoCentavos = montoDeclaradoCentavos;
        DiferenciaCentavos = montoDeclaradoCentavos - montoEsperadoCentavos;
        FechaCierre = fechaCierre ?? DateTime.UtcNow;
        Estado = EstadoCaja.CERRADA;
    }
}
