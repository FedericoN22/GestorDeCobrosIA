using Kiosk.Domain.Ventas;

namespace Kiosk.Domain.Tests;

public class CajaTests
{
    [Fact]
    public void Abrir_ConDatosValidos_AsignaPropiedades()
    {
        var caja = Caja.Abrir(Guid.NewGuid(), Guid.NewGuid(), 20000);

        Assert.Equal(EstadoCaja.ABIERTA, caja.Estado);
        Assert.Equal(20000, caja.MontoInicialCentavos);
        Assert.Null(caja.FechaCierre);
    }

    [Fact]
    public void Abrir_ConIdYFechaExplicitos_LosRespeta()
    {
        var id = Guid.NewGuid();
        var fecha = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc);

        var caja = Caja.Abrir(Guid.NewGuid(), Guid.NewGuid(), 0, id, fecha);

        Assert.Equal(id, caja.Id);
        Assert.Equal(fecha, caja.FechaApertura);
    }

    [Fact]
    public void Cerrar_ConFechaExplicita_LaRespeta()
    {
        var caja = Caja.Abrir(Guid.NewGuid(), Guid.NewGuid(), 0);
        var fecha = new DateTime(2026, 8, 1, 19, 0, 0, DateTimeKind.Utc);

        caja.Cerrar(50000, 50000, fecha);

        Assert.Equal(fecha, caja.FechaCierre);
    }

    [Fact]
    public void Abrir_ConMontoInicialNegativo_LanzaError()
    {
        AssertHelper.ThrowsDomain(
            "CAJA_MONTO_INICIAL_INVALIDO",
            () => Caja.Abrir(Guid.NewGuid(), Guid.NewGuid(), -1));
    }

    [Fact]
    public void Cerrar_CalculaDiferencia()
    {
        var caja = Caja.Abrir(Guid.NewGuid(), Guid.NewGuid(), 0);
        caja.Cerrar(50000, 48500);

        Assert.Equal(EstadoCaja.CERRADA, caja.Estado);
        Assert.Equal(50000, caja.MontoEsperadoCentavos);
        Assert.Equal(48500, caja.MontoDeclaradoCentavos);
        Assert.Equal(-1500, caja.DiferenciaCentavos);
        Assert.NotNull(caja.FechaCierre);
    }

    [Fact]
    public void Cerrar_DosVeces_LanzaError()
    {
        var caja = Caja.Abrir(Guid.NewGuid(), Guid.NewGuid(), 0);
        caja.Cerrar(50000, 50000);

        AssertHelper.ThrowsDomain("CAJA_YA_CERRADA", () => caja.Cerrar(50000, 50000));
    }

    [Fact]
    public void Cerrar_ConMontoNegativo_LanzaError()
    {
        var caja = Caja.Abrir(Guid.NewGuid(), Guid.NewGuid(), 0);

        AssertHelper.ThrowsDomain("CAJA_MONTOS_INVALIDOS", () => caja.Cerrar(-1, 50000));
    }
}
