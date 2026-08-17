using Kiosk.Application.Intenciones;
using Kiosk.Application.Puertos.Integraciones;
using Kiosk.Domain.Common;

namespace Kiosk.Ia.Tests;

public class StubParserTests
{
    private readonly StubParser _parser = new();

    private static string N(string texto) => Normalizacion.Normalizar(texto);

    private Task<ResultadoParseo> Parsear(string texto)
        => _parser.ParsearAsync(N(texto));

    [Fact]
    public async Task ConsultarStock_ConPresentacion_ReconoceProductoYPresentacion()
    {
        var r = await Parsear("Cuánto stock hay de Coca Cola 1.5L?");

        Assert.False(r.EsFallo);
        Assert.Equal(AccionIntencion.CONSULTAR_STOCK, r.Comando!.Accion);
        Assert.Equal("COCA COLA", r.Comando.Parametros.Producto);
        Assert.Equal("1.5L", r.Comando.Parametros.Presentacion);
    }

    [Fact]
    public async Task ConsultarStock_SinPresentacion_DejaPresentacionEnNull()
    {
        var r = await Parsear("Cuánto stock de Coca Cola");

        Assert.Equal(AccionIntencion.CONSULTAR_STOCK, r.Comando!.Accion);
        Assert.Equal("COCA COLA", r.Comando.Parametros.Producto);
        Assert.Null(r.Comando.Parametros.Presentacion);
    }

    [Theory]
    [InlineData("Cuánto sale Coca Cola 1.5L?", "COCA COLA", "1.5L")]
    [InlineData("Cuánto cuesta Fanta 600ml", "FANTA", "600ML")]
    [InlineData("Precio de Sprite 2L", "SPRITE", "2L")]
    public async Task ConsultarPrecio_ReconoceLasVariantes(string texto, string producto, string presentacion)
    {
        var r = await Parsear(texto);

        Assert.False(r.EsFallo);
        Assert.Equal(AccionIntencion.CONSULTAR_PRECIO, r.Comando!.Accion);
        Assert.Equal(producto, r.Comando!.Parametros.Producto);
        Assert.Equal(presentacion, r.Comando.Parametros.Presentacion);
    }

    [Theory]
    [InlineData("listar")]
    [InlineData("listar productos")]
    [InlineData("lista de productos")]
    [InlineData("qué productos hay")]
    public async Task Listar_ReconoceLasVariantes(string texto)
    {
        var r = await Parsear(texto);

        Assert.False(r.EsFallo);
        Assert.Equal(AccionIntencion.LISTAR_PRODUCTOS, r.Comando!.Accion);
    }

    [Fact]
    public async Task Agregar_ConCantidadYDetalle_ReconoceCantidad()
    {
        var r = await Parsear("Agregar Coca Cola 1.5L, cantidad 10");

        Assert.Equal(AccionIntencion.AGREGAR_STOCK, r.Comando!.Accion);
        Assert.Equal("COCA COLA", r.Comando.Parametros.Producto);
        Assert.Equal("1.5L", r.Comando.Parametros.Presentacion);
        Assert.Equal(10, r.Comando.Parametros.Cantidad);
    }

    [Fact]
    public async Task Agregar_ConCosto_ReconoceTipoPrecioCosto()
    {
        var r = await Parsear("Agregar Coca Cola 1.5L, cantidad 10, costo 50");

        Assert.Equal(AccionIntencion.AGREGAR_STOCK, r.Comando!.Accion);
        Assert.Equal(10, r.Comando.Parametros.Cantidad);
        Assert.Equal(50, r.Comando.Parametros.Precio);
        Assert.Equal(TipoPrecio.COSTO, r.Comando.Parametros.TipoPrecio);
    }

    [Fact]
    public async Task Agregar_SinCantidad_ReportaCantidadFaltante()
    {
        var r = await Parsear("Agregar Coca Cola 1.5L");

        Assert.Equal(AccionIntencion.AGREGAR_STOCK, r.Comando!.Accion);
        Assert.Null(r.Comando.Parametros.Cantidad);
        Assert.Contains("cantidad", r.Comando.CamposFaltantes);
    }

    [Fact]
    public async Task ModificarPrecio_ReconoceNuevoPrecio()
    {
        var r = await Parsear("Cambiar precio de Coca Cola 1.5L a 100");

        Assert.Equal(AccionIntencion.MODIFICAR_PRECIO, r.Comando!.Accion);
        Assert.Equal("COCA COLA", r.Comando.Parametros.Producto);
        Assert.Equal("1.5L", r.Comando.Parametros.Presentacion);
        Assert.Equal(100, r.Comando.Parametros.Precio);
    }

    [Fact]
    public async Task CrearProducto_ConPresentacionYPrecio_ReconoceTodo()
    {
        var r = await Parsear("Crear producto Coca Cola, presentación 1.5L, precio 100");

        Assert.Equal(AccionIntencion.CREAR_PRODUCTO, r.Comando!.Accion);
        Assert.Equal("COCA COLA", r.Comando.Parametros.Producto);
        Assert.Equal("1.5L", r.Comando.Parametros.Presentacion);
        Assert.Equal(100, r.Comando.Parametros.Precio);
    }

    [Fact]
    public async Task CrearProducto_SoloNombre_DejaPresentacionEnNull()
    {
        var r = await Parsear("Crear Coca Cola");

        Assert.Equal(AccionIntencion.CREAR_PRODUCTO, r.Comando!.Accion);
        Assert.Equal("COCA COLA", r.Comando.Parametros.Producto);
        Assert.Null(r.Comando.Parametros.Presentacion);
        Assert.Empty(r.Comando.CamposFaltantes);
    }

    [Fact]
    public async Task CrearProducto_ConPresentacionSinPrecio_ReportaPrecioFaltante()
    {
        var r = await Parsear("Crear producto Coca Cola, presentación 1.5L");

        Assert.Equal(AccionIntencion.CREAR_PRODUCTO, r.Comando!.Accion);
        Assert.Equal("1.5L", r.Comando.Parametros.Presentacion);
        Assert.Contains("precio", r.Comando.CamposFaltantes);
    }

    [Fact]
    public async Task Eliminar_ReconoceProductoYPresentacion()
    {
        var r = await Parsear("Eliminar Coca Cola 1.5L");

        Assert.Equal(AccionIntencion.ELIMINAR_PRODUCTO, r.Comando!.Accion);
        Assert.Equal("COCA COLA", r.Comando.Parametros.Producto);
        Assert.Equal("1.5L", r.Comando.Parametros.Presentacion);
    }

    [Fact]
    public async Task NombreDeProductoConNumero_SeparaLaPresentacionCorrectamente()
    {
        var r = await Parsear("Cuánto stock hay de Agua Villavicencio 2L");

        Assert.Equal(AccionIntencion.CONSULTAR_STOCK, r.Comando!.Accion);
        Assert.Equal("AGUA VILLAVICENCIO", r.Comando.Parametros.Producto);
        Assert.Equal("2L", r.Comando.Parametros.Presentacion);
    }

    [Fact]
    public async Task MultiComando_SeDetecta()
    {
        var r = await Parsear("Agregar Coca Cola cantidad 5 y eliminar Pepsi");

        Assert.True(r.EsMultiComando);
        Assert.Null(r.Comando);
    }

    [Fact]
    public async Task MensajeSinSentido_Falla()
    {
        var r = await Parsear("zzzzzzz");

        Assert.True(r.EsFallo);
        Assert.Null(r.Comando);
    }
}
