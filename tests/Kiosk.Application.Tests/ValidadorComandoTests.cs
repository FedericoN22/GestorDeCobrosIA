using Kiosk.Application.CasosUso.Whatsapp;
using Kiosk.Application.Intenciones;

namespace Kiosk.Application.Tests;

public class ValidadorComandoTests
{
    private static StructuredCommand Comando(
        AccionIntencion accion,
        string? producto = null,
        string? presentacion = null,
        int? cantidad = null,
        int? precio = null)
        => new(
            1,
            accion,
            presentacion is null ? "PRODUCTO" : "PRESENTACION",
            new ParametrosComando(producto, presentacion, cantidad, precio, TipoPrecio.NO_INDICADO, null, producto),
            0.9m,
            [],
            [],
            producto ?? "");

    [Fact]
    public void ConsultarStock_SinProducto_ReportaProductoFaltante()
    {
        var faltantes = ValidadorComando.CalcularFaltantes(Comando(AccionIntencion.CONSULTAR_STOCK));

        Assert.Equal(["producto"], faltantes);
    }

    [Fact]
    public void ConsultarStock_ConProducto_NoReportaFaltantes()
    {
        var faltantes = ValidadorComando.CalcularFaltantes(Comando(AccionIntencion.CONSULTAR_STOCK, "COCA"));

        Assert.Empty(faltantes);
    }

    [Fact]
    public void ConsultarPrecio_SinProducto_ReportaProductoFaltante()
    {
        var faltantes = ValidadorComando.CalcularFaltantes(Comando(AccionIntencion.CONSULTAR_PRECIO));

        Assert.Equal(["producto"], faltantes);
    }

    [Fact]
    public void AgregarStock_SinDatos_ReportaLosTresFaltantes()
    {
        var faltantes = ValidadorComando.CalcularFaltantes(Comando(AccionIntencion.AGREGAR_STOCK));

        Assert.Equal(["producto", "presentacion", "cantidad"], faltantes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void AgregarStock_CantidadNoPositiva_ReportaCantidadFaltante(int cantidad)
    {
        var faltantes = ValidadorComando.CalcularFaltantes(
            Comando(AccionIntencion.AGREGAR_STOCK, "COCA", "1.5L", cantidad));

        Assert.Equal(["cantidad"], faltantes);
    }

    [Fact]
    public void AgregarStock_Completo_NoReportaFaltantes()
    {
        var faltantes = ValidadorComando.CalcularFaltantes(
            Comando(AccionIntencion.AGREGAR_STOCK, "COCA", "1.5L", 10));

        Assert.Empty(faltantes);
    }

    [Fact]
    public void CrearProducto_SoloNombre_NoReportaFaltantes()
    {
        var faltantes = ValidadorComando.CalcularFaltantes(Comando(AccionIntencion.CREAR_PRODUCTO, "SPRITE"));

        Assert.Empty(faltantes);
    }

    [Fact]
    public void CrearProducto_ConPresentacionSinPrecio_ReportaPrecioFaltante()
    {
        var faltantes = ValidadorComando.CalcularFaltantes(
            Comando(AccionIntencion.CREAR_PRODUCTO, "SPRITE", "2L"));

        Assert.Equal(["precio"], faltantes);
    }

    [Fact]
    public void CrearProducto_ConPresentacionYPrecio_NoReportaFaltantes()
    {
        var faltantes = ValidadorComando.CalcularFaltantes(
            Comando(AccionIntencion.CREAR_PRODUCTO, "SPRITE", "2L", precio: 100));

        Assert.Empty(faltantes);
    }

    [Fact]
    public void ModificarPrecio_SinPrecio_ReportaPrecioFaltante()
    {
        var faltantes = ValidadorComando.CalcularFaltantes(
            Comando(AccionIntencion.MODIFICAR_PRECIO, "COCA", "1.5L"));

        Assert.Equal(["precio"], faltantes);
    }

    [Fact]
    public void ModificarPrecio_Completo_NoReportaFaltantes()
    {
        var faltantes = ValidadorComando.CalcularFaltantes(
            Comando(AccionIntencion.MODIFICAR_PRECIO, "COCA", "1.5L", precio: 100));

        Assert.Empty(faltantes);
    }

    [Fact]
    public void EliminarProducto_SinProducto_ReportaProductoFaltante()
    {
        var faltantes = ValidadorComando.CalcularFaltantes(Comando(AccionIntencion.ELIMINAR_PRODUCTO));

        Assert.Equal(["producto"], faltantes);
    }

    [Fact]
    public void EliminarProducto_ConProducto_NoReportaFaltantes()
    {
        var faltantes = ValidadorComando.CalcularFaltantes(Comando(AccionIntencion.ELIMINAR_PRODUCTO, "COCA"));

        Assert.Empty(faltantes);
    }

    [Fact]
    public void AccionNoContemplada_NoReportaFaltantes()
    {
        var faltantes = ValidadorComando.CalcularFaltantes(Comando((AccionIntencion)99));

        Assert.Empty(faltantes);
    }
}
