using Kiosk.Domain.Ventas;

namespace Kiosk.Domain.Tests;

public class VentaTests
{
    private const int PrecioUnitario = 4200;

    private static Venta CrearVenta()
        => Venta.Crear(Guid.NewGuid(), Guid.NewGuid(), 1, DateTime.UtcNow);

    [Fact]
    public void Crear_AsignaPropiedadesBasicas()
    {
        var venta = Venta.Crear(Guid.NewGuid(), Guid.NewGuid(), 5, DateTime.UtcNow, clientGenerated: true);

        Assert.Equal(5, venta.Numero);
        Assert.True(venta.ClientGenerated);
        Assert.Equal(0, venta.TotalCentavos);
        Assert.Empty(venta.Lineas);
        Assert.Empty(venta.Pagos);
    }

    [Fact]
    public void AgregarLinea_CalculaSubtotalYTotal()
    {
        var venta = CrearVenta();
        venta.AgregarLinea(Guid.NewGuid(), "Coca Cola", "2.25L", 2, PrecioUnitario);
        venta.AgregarLinea(Guid.NewGuid(), "Coca Cola", "600ml", 1, 2500);

        Assert.Equal(2 * PrecioUnitario + 2500, venta.TotalCentavos);
        Assert.Equal(2, venta.Lineas.Count);
    }

    [Fact]
    public void AgregarLinea_ConCantidadCero_LanzaError()
    {
        var venta = CrearVenta();

        AssertHelper.ThrowsDomain(
            "VENTA_CANTIDAD_INVALIDA",
            () => venta.AgregarLinea(Guid.NewGuid(), "Coca Cola", "2.25L", 0, PrecioUnitario));
    }

    [Fact]
    public void AgregarLinea_ConPrecioInvalido_LanzaError()
    {
        var venta = CrearVenta();

        AssertHelper.ThrowsDomain(
            "VENTA_PRECIO_INVALIDO",
            () => venta.AgregarLinea(Guid.NewGuid(), "Coca Cola", "2.25L", 1, 0));
    }

    [Fact]
    public void AgregarLinea_SinSnapshotsDeNombres_LanzaError()
    {
        var venta = CrearVenta();

        AssertHelper.ThrowsDomain(
            "VENTA_SNAPSHOT_INVALIDO",
            () => venta.AgregarLinea(Guid.NewGuid(), "  ", "2.25L", 1, PrecioUnitario));
    }

    [Fact]
    public void AgregarPago_ConMontoInvalido_LanzaError()
    {
        var venta = CrearVenta();

        AssertHelper.ThrowsDomain(
            "PAGO_MONTO_INVALIDO",
            () => venta.AgregarPago(MedioPago.EFECTIVO, 0));
    }

    [Fact]
    public void TotalPagado_SumaTodosLosPagos()
    {
        var venta = CrearVenta();
        venta.AgregarPago(MedioPago.EFECTIVO, 3000);
        venta.AgregarPago(MedioPago.TRANSFERENCIA_QR, 1200);

        Assert.Equal(4200, venta.TotalPagadoCentavos);
    }

    [Fact]
    public void ValidarPagosCompletos_ConPagoMenor_LanzaError()
    {
        var venta = CrearVenta();
        venta.AgregarLinea(Guid.NewGuid(), "Coca Cola", "2.25L", 2, PrecioUnitario);
        venta.AgregarPago(MedioPago.EFECTIVO, 1000);

        AssertHelper.ThrowsDomain("VENTA_PAGOS_INCOMPLETOS", venta.ValidarPagosCompletos);
    }

    [Fact]
    public void ValidarPagosCompletos_ConPagoMayorOIgual_NoLanzaError()
    {
        var venta = CrearVenta();
        venta.AgregarLinea(Guid.NewGuid(), "Coca Cola", "2.25L", 2, PrecioUnitario);
        venta.AgregarPago(MedioPago.EFECTIVO, PrecioUnitario * 2);

        var ex = Record.Exception(venta.ValidarPagosCompletos);
        Assert.Null(ex);
    }

    [Fact]
    public void AgregarLinea_GuardaPrecioComoSnapshot()
    {
        var venta = CrearVenta();
        venta.AgregarLinea(Guid.NewGuid(), "Coca Cola", "2.25L", 1, PrecioUnitario);

        var linea = venta.Lineas[0];
        Assert.Equal(PrecioUnitario, linea.PrecioUnitarioCentavos);
        Assert.Equal(PrecioUnitario, linea.SubtotalCentavos);
    }
}
