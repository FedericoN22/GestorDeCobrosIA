using Kiosk.Domain.Catalogos;

namespace Kiosk.Domain.Tests;

public class PresentacionTests
{
    [Fact]
    public void Crear_ConDatosValidos_AsignaPropiedades()
    {
        var presentacion = Presentacion.Crear(Guid.NewGuid(), "  2.25L  ", 4200, 3500, " 7790000000001 ");

        Assert.Equal("2.25L", presentacion.Nombre);
        Assert.Equal(4200, presentacion.PrecioVentaCentavos);
        Assert.Equal(3500, presentacion.PrecioCostoCentavos);
        Assert.Equal("7790000000001", presentacion.CodigoBarras);
        Assert.Equal(0, presentacion.StockActual);
        Assert.True(presentacion.Activa);
    }

    [Fact]
    public void Crear_PrecioCostoOpcional_Y_CodigoBarrasOpcional()
    {
        var presentacion = Presentacion.Crear(Guid.NewGuid(), "2.25L", 4200);

        Assert.Null(presentacion.PrecioCostoCentavos);
        Assert.Null(presentacion.CodigoBarras);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_ConNombreVacio_LanzaError(string? nombre)
    {
        AssertHelper.ThrowsDomain(
            "PRESENTACION_NOMBRE_REQUERIDO",
            () => Presentacion.Crear(Guid.NewGuid(), nombre!, 4200));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Crear_PrecioVentaMenorOIgualACero_LanzaError(int precio)
    {
        AssertHelper.ThrowsDomain(
            "PRECIO_VENTA_INVALIDO",
            () => Presentacion.Crear(Guid.NewGuid(), "2.25L", precio));
    }

    [Fact]
    public void Crear_PrecioCostoNegativo_LanzaError()
    {
        AssertHelper.ThrowsDomain(
            "PRECIO_COSTO_INVALIDO",
            () => Presentacion.Crear(Guid.NewGuid(), "2.25L", 4200, -1));
    }

    [Fact]
    public void Crear_CodigoBarrasMayorA32Caracteres_LanzaError()
    {
        AssertHelper.ThrowsDomain(
            "CODIGO_BARRAS_LARGO",
            () => Presentacion.Crear(Guid.NewGuid(), "2.25L", 4200, null, new string('9', 33)));
    }

    [Fact]
    public void CambiarPrecioVenta_ActualizaPrecio()
    {
        var presentacion = Presentacion.Crear(Guid.NewGuid(), "2.25L", 4200);
        presentacion.CambiarPrecioVenta(4500);

        Assert.Equal(4500, presentacion.PrecioVentaCentavos);
    }

    [Fact]
    public void CambiarPrecioVenta_Invalido_LanzaError()
    {
        var presentacion = Presentacion.Crear(Guid.NewGuid(), "2.25L", 4200);

        AssertHelper.ThrowsDomain("PRECIO_VENTA_INVALIDO", () => presentacion.CambiarPrecioVenta(0));
    }

    [Fact]
    public void CambiarPrecioCosto_Negativo_LanzaError()
    {
        var presentacion = Presentacion.Crear(Guid.NewGuid(), "2.25L", 4200);

        AssertHelper.ThrowsDomain("PRECIO_COSTO_INVALIDO", () => presentacion.CambiarPrecioCosto(-5));
    }

    [Fact]
    public void ActualizarStock_Negativo_LanzaError()
    {
        var presentacion = Presentacion.Crear(Guid.NewGuid(), "2.25L", 4200);

        AssertHelper.ThrowsDomain("STOCK_NEGATIVO", () => presentacion.ActualizarStock(-1));
    }

    [Fact]
    public void ActualizarStock_ConValorPositivo_Actualiza()
    {
        var presentacion = Presentacion.Crear(Guid.NewGuid(), "2.25L", 4200);
        presentacion.ActualizarStock(12);

        Assert.Equal(12, presentacion.StockActual);
    }

    [Fact]
    public void ConfigurarStockMinimo_Negativo_LanzaError()
    {
        var presentacion = Presentacion.Crear(Guid.NewGuid(), "2.25L", 4200);

        AssertHelper.ThrowsDomain("STOCK_MINIMO_INVALIDO", () => presentacion.ConfigurarStockMinimo(-1));
    }

    [Fact]
    public void ConfigurarStockMinimo_ConValorPositivo_Actualiza()
    {
        var presentacion = Presentacion.Crear(Guid.NewGuid(), "2.25L", 4200);
        presentacion.ConfigurarStockMinimo(10);

        Assert.Equal(10, presentacion.StockMinimo);
    }

    [Fact]
    public void Desactivar_PoneActivaEnFalse()
    {
        var presentacion = Presentacion.Crear(Guid.NewGuid(), "2.25L", 4200);
        presentacion.Desactivar();

        Assert.False(presentacion.Activa);
    }
}
