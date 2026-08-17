using Kiosk.Application.CasosUso.Catalogos;
using Kiosk.Application.CasosUso.Stock;
using Kiosk.Application.CasosUso.Whatsapp;
using Kiosk.Application.Intenciones;
using Kiosk.Application.Tests.TestDoubles;
using Kiosk.Domain.Catalogos;
using Kiosk.Domain.Common;
using Kiosk.Domain.Stock;

namespace Kiosk.Application.Tests;

public class EjecutorAccionesTests
{
    private static readonly Guid ComercioId = Guid.NewGuid();

    private sealed class Contexto
    {
        public Contexto()
        {
            Uow = new FakeUnitOfWork(() => Ledger.Commit());

            var stock = new ServicioStock(Productos, Ledger, Auditoria, Uow);
            var productos = new ServicioProductos(Productos, Categorias, Auditoria, Uow);
            Resolvedor = new ResolvedorCatalogos(Productos);
            Ejecutor = new EjecutorAcciones(stock, productos, Resolvedor, Ledger);
        }

        public FakeProductRepository Productos { get; } = new();
        public FakeCategoriaRepository Categorias { get; } = new();
        public FakeStockLedger Ledger { get; } = new();
        public FakeUnitOfWork Uow { get; }
        public FakeAuditoriaRepository Auditoria { get; } = new();
        public ResolvedorCatalogos Resolvedor { get; }
        public EjecutorAcciones Ejecutor { get; }
    }

    private static StructuredCommand Comando(AccionIntencion accion, string? producto = null, string? presentacion = null, int? cantidad = null, int? precio = null, TipoPrecio tipoPrecio = TipoPrecio.NO_INDICADO)
        => new(
            1,
            accion,
            presentacion is null ? "PRODUCTO" : "PRESENTACION",
            new ParametrosComando(producto, presentacion, cantidad, precio, tipoPrecio, null, producto),
            0.95m,
            [],
            [],
            producto ?? "");

    private static (Producto Producto, Presentacion Presentacion) SeedProducto(Contexto ctx, string nombre, string presentacion)
    {
        var producto = Producto.Crear(ComercioId, null, nombre);
        var pres = producto.AgregarPresentacion(presentacion, 1500);
        ctx.Productos.Seed(producto);
        return (producto, pres);
    }

    [Fact]
    public async Task ConsultarStock_DevuelveStockConsultado()
    {
        var ctx = new Contexto();
        var (producto, presentacion) = SeedProducto(ctx, "Coca Cola", "1.5L");
        ctx.Ledger.Seed(MovimientoStock.EntradaManual(presentacion.Id, 9, null, Canal.WEB));

        var resultado = await ctx.Ejecutor.EjecutarAsync(
            ComercioId, Comando(AccionIntencion.CONSULTAR_STOCK, "COCA COLA", "1.5L"),
            new CoincidenciaPresentacion(producto, presentacion), "whatsapp");

        Assert.True(resultado.IsSuccess);
        Assert.Equal(RespuestasBot.StockConsultado("Coca Cola", "1.5L", 9, null), resultado.Value);
    }

    [Fact]
    public async Task ConsultarPrecio_DevuelvePrecioConsultado()
    {
        var ctx = new Contexto();
        var (producto, presentacion) = SeedProducto(ctx, "Coca Cola", "1.5L");

        var resultado = await ctx.Ejecutor.EjecutarAsync(
            ComercioId, Comando(AccionIntencion.CONSULTAR_PRECIO, "COCA COLA", "1.5L"),
            new CoincidenciaPresentacion(producto, presentacion), "whatsapp");

        Assert.True(resultado.IsSuccess);
        Assert.Equal(RespuestasBot.PrecioConsultado("Coca Cola", "1.5L", 1500), resultado.Value);
    }

    [Fact]
    public async Task Listar_ConProductos_DevuelveListado()
    {
        var ctx = new Contexto();
        SeedProducto(ctx, "Coca Cola", "1.5L");

        var resultado = await ctx.Ejecutor.EjecutarAsync(
            ComercioId, Comando(AccionIntencion.LISTAR_PRODUCTOS), null, "whatsapp");

        Assert.True(resultado.IsSuccess);
        Assert.Contains("Coca Cola 1.5L", resultado.Value);
    }

    [Fact]
    public async Task Listar_SinProductos_DevuelveMensajeVacio()
    {
        var ctx = new Contexto();

        var resultado = await ctx.Ejecutor.EjecutarAsync(
            ComercioId, Comando(AccionIntencion.LISTAR_PRODUCTOS), null, "whatsapp");

        Assert.True(resultado.IsSuccess);
        Assert.Equal("Todavía no hay productos cargados en el catálogo.", resultado.Value);
    }

    [Fact]
    public async Task AgregarStock_EjecutaEntradaManualYResponde()
    {
        var ctx = new Contexto();
        var (producto, presentacion) = SeedProducto(ctx, "Coca Cola", "1.5L");

        var resultado = await ctx.Ejecutor.EjecutarAsync(
            ComercioId, Comando(AccionIntencion.AGREGAR_STOCK, "COCA COLA", "1.5L", cantidad: 10),
            new CoincidenciaPresentacion(producto, presentacion), "whatsapp");

        Assert.True(resultado.IsSuccess);
        Assert.Equal(RespuestasBot.StockAgregado("Coca Cola", "1.5L", 10, 10), resultado.Value);
        Assert.Equal(10, await ctx.Ledger.CalcularStockAsync(presentacion.Id));
    }

    [Fact]
    public async Task AgregarStock_ConPrecioCosto_ActualizaPrecioCosto()
    {
        var ctx = new Contexto();
        var (producto, presentacion) = SeedProducto(ctx, "Coca Cola", "1.5L");

        var resultado = await ctx.Ejecutor.EjecutarAsync(
            ComercioId, Comando(AccionIntencion.AGREGAR_STOCK, "COCA COLA", "1.5L", cantidad: 10, precio: 42, TipoPrecio.COSTO),
            new CoincidenciaPresentacion(producto, presentacion), "whatsapp");

        Assert.True(resultado.IsSuccess);
        Assert.Equal(4200, presentacion.PrecioCostoCentavos);
    }

    [Fact]
    public async Task CrearProducto_SoloNombre_CreaProductoSinPresentacion()
    {
        var ctx = new Contexto();

        var resultado = await ctx.Ejecutor.EjecutarAsync(
            ComercioId, Comando(AccionIntencion.CREAR_PRODUCTO, "SPRITE"), null, "whatsapp");

        Assert.True(resultado.IsSuccess);
        Assert.Equal(RespuestasBot.ProductoCreado("SPRITE", null, null), resultado.Value);
        Assert.Contains(ctx.Productos.Productos, p => p.Nombre == "SPRITE");
    }

    [Fact]
    public async Task CrearProducto_ConPresentacionYPrecio_CreaTodo()
    {
        var ctx = new Contexto();

        var resultado = await ctx.Ejecutor.EjecutarAsync(
            ComercioId, Comando(AccionIntencion.CREAR_PRODUCTO, "SPRITE", "2L", precio: 100), null, "whatsapp");

        Assert.True(resultado.IsSuccess);
        Assert.Equal(RespuestasBot.ProductoCreado("SPRITE", "2L", 10000), resultado.Value);
        var producto = ctx.Productos.Productos.Single();
        var pres = producto.Presentaciones.Single();
        Assert.Equal(10000, pres.PrecioVentaCentavos);
    }

    [Fact]
    public async Task CrearProducto_ConPresentacionSinPrecio_FallaPrecioRequerido()
    {
        var ctx = new Contexto();

        var resultado = await ctx.Ejecutor.EjecutarAsync(
            ComercioId, Comando(AccionIntencion.CREAR_PRODUCTO, "SPRITE", "2L"), null, "whatsapp");

        Assert.False(resultado.IsSuccess);
        Assert.Equal("PRECIO_REQUERIDO", resultado.Error?.Code);
    }

    [Fact]
    public async Task ModificarPrecio_ConviertePesosACentavos_SinDobleConversion()
    {
        var ctx = new Contexto();
        var (producto, presentacion) = SeedProducto(ctx, "Coca Cola", "1.5L");

        var resultado = await ctx.Ejecutor.EjecutarAsync(
            ComercioId, Comando(AccionIntencion.MODIFICAR_PRECIO, "COCA COLA", "1.5L", precio: 4200),
            new CoincidenciaPresentacion(producto, presentacion), "whatsapp");

        Assert.True(resultado.IsSuccess);
        Assert.Equal(420000, presentacion.PrecioVentaCentavos);
        Assert.Equal(RespuestasBot.PrecioModificado("Coca Cola", "1.5L", 420000), resultado.Value);
    }

    [Fact]
    public async Task Eliminar_DesactivaLaPresentacion()
    {
        var ctx = new Contexto();
        var (producto, presentacion) = SeedProducto(ctx, "Coca Cola", "1.5L");

        var resultado = await ctx.Ejecutor.EjecutarAsync(
            ComercioId, Comando(AccionIntencion.ELIMINAR_PRODUCTO, "COCA COLA", "1.5L"),
            new CoincidenciaPresentacion(producto, presentacion), "whatsapp");

        Assert.True(resultado.IsSuccess);
        Assert.False(presentacion.Activa);
    }

    [Fact]
    public async Task AccionDesconocida_Falla()
    {
        var ctx = new Contexto();

        var resultado = await ctx.Ejecutor.EjecutarAsync(
            ComercioId, Comando((AccionIntencion)99), null, "whatsapp");

        Assert.False(resultado.IsSuccess);
        Assert.Equal("ACCION_DESCONOCIDA", resultado.Error?.Code);
    }

    [Fact]
    public async Task AgregarStock_PresentacionInexistente_PropagaError()
    {
        var ctx = new Contexto();
        var (producto, _) = SeedProducto(ctx, "Coca Cola", "1.5L");
        var fantasma = Presentacion.Crear(producto.Id, "Inexistente", 1000);

        var resultado = await ctx.Ejecutor.EjecutarAsync(
            ComercioId, Comando(AccionIntencion.AGREGAR_STOCK, "COCA COLA", "Inexistente", cantidad: 10),
            new CoincidenciaPresentacion(producto, fantasma), "whatsapp");

        Assert.False(resultado.IsSuccess);
        Assert.Equal("PRESENTACION_NO_ENCONTRADA", resultado.Error?.Code);
    }
}
