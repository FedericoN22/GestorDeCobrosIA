using Kiosk.Domain.Common;
using Kiosk.Domain.Stock;

namespace Kiosk.Domain.Tests;

public class MovimientoStockTests
{
    private static readonly Guid PresentacionId = Guid.NewGuid();
    private static readonly Guid UsuarioId = Guid.NewGuid();

    [Fact]
    public void EntradaManual_ConCantidadPositiva_CreaMovimiento()
    {
        var movimiento = MovimientoStock.EntradaManual(PresentacionId, 12, UsuarioId, Canal.WEB);

        Assert.Equal(TipoMovimiento.ENTRADA_MANUAL, movimiento.Tipo);
        Assert.Equal(12, movimiento.Cantidad);
        Assert.Equal(UsuarioId, movimiento.UsuarioId);
        Assert.Null(movimiento.Motivo);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void EntradaManual_ConCantidadNoPositiva_LanzaError(int cantidad)
    {
        AssertHelper.ThrowsDomain(
            "STOCK_CANTIDAD_INVALIDA",
            () => MovimientoStock.EntradaManual(PresentacionId, cantidad, UsuarioId, Canal.WEB));
    }

    [Fact]
    public void Ajuste_ConCantidadCero_LanzaError()
    {
        AssertHelper.ThrowsDomain(
            "STOCK_CANTIDAD_INVALIDA",
            () => MovimientoStock.Ajuste(PresentacionId, 0, "Rotura", UsuarioId, Canal.WEB));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Ajuste_SinMotivo_LanzaError(string? motivo)
    {
        AssertHelper.ThrowsDomain(
            "STOCK_MOTIVO_REQUERIDO",
            () => MovimientoStock.Ajuste(PresentacionId, -2, motivo!, UsuarioId, Canal.WEB));
    }

    [Fact]
    public void Ajuste_ConMotivo_CreaMovimientoConMotivoRecortado()
    {
        var movimiento = MovimientoStock.Ajuste(PresentacionId, -2, "  Vencido  ", UsuarioId, Canal.WEB);

        Assert.Equal(TipoMovimiento.AJUSTE, movimiento.Tipo);
        Assert.Equal(-2, movimiento.Cantidad);
        Assert.Equal("Vencido", movimiento.Motivo);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Venta_ConCantidadNoPositiva_LanzaError(int cantidad)
    {
        AssertHelper.ThrowsDomain(
            "STOCK_CANTIDAD_INVALIDA",
            () => MovimientoStock.Venta(PresentacionId, cantidad, Guid.NewGuid(), Canal.POS));
    }

    [Fact]
    public void Venta_RegistraCantidadNegativaConReferenciaAVenta()
    {
        var ventaId = Guid.NewGuid();
        var movimiento = MovimientoStock.Venta(PresentacionId, 3, ventaId, Canal.POS);

        Assert.Equal(TipoMovimiento.VENTA, movimiento.Tipo);
        Assert.Equal(-3, movimiento.Cantidad);
        Assert.Equal(ventaId, movimiento.VentaId);
    }

    [Fact]
    public void Devolucion_ConCantidadPositiva_CreaMovimiento()
    {
        var movimiento = MovimientoStock.Devolucion(PresentacionId, 1, Guid.NewGuid(), Canal.POS);

        Assert.Equal(TipoMovimiento.DEVOLUCION, movimiento.Tipo);
        Assert.Equal(1, movimiento.Cantidad);
    }

    [Fact]
    public void Devolucion_ConCantidadCero_LanzaError()
    {
        AssertHelper.ThrowsDomain(
            "STOCK_CANTIDAD_INVALIDA",
            () => MovimientoStock.Devolucion(PresentacionId, 0, Guid.NewGuid(), Canal.POS));
    }
}
