using Kiosk.Application.CasosUso.Whatsapp;
using Kiosk.Application.Intenciones;
using Kiosk.Application.Tests.TestDoubles;
using Kiosk.Domain.Catalogos;

namespace Kiosk.Application.Tests;

public class ResolvedorCatalogosTests
{
    private static readonly Guid ComercioId = Guid.NewGuid();

    private static StructuredCommand Comando(AccionIntencion accion, string? producto = null, string? presentacion = null)
        => new(
            1,
            accion,
            presentacion is null ? "PRODUCTO" : "PRESENTACION",
            new ParametrosComando(producto, presentacion, null, null, TipoPrecio.NO_INDICADO, null, producto),
            0.9m,
            [],
            [],
            producto ?? "");

    private static (ResolvedorCatalogos Resolvedor, FakeProductRepository Productos, Producto Producto, Presentacion Pres1) CrearContexto(
        bool conSegundaPresentacion = false,
        string? segundaPresentacion = null)
    {
        var productos = new FakeProductRepository();
        var producto = Producto.Crear(ComercioId, null, "Coca Cola");
        var pres1 = producto.AgregarPresentacion("1.5L", 1500);
        if (conSegundaPresentacion)
        {
            producto.AgregarPresentacion(segundaPresentacion ?? "600ML", 900);
        }

        productos.Seed(producto);
        return (new ResolvedorCatalogos(productos), productos, producto, pres1);
    }

    [Fact]
    public async Task BuscarAsync_SinProducto_NoBusca()
    {
        var (resolvedor, _, _, _) = CrearContexto();

        var resultado = await resolvedor.BuscarAsync(ComercioId, null, null);

        Assert.False(resultado.Buscado);
    }

    [Fact]
    public async Task Resolver_ListarProductos_NoAplica()
    {
        var (resolvedor, _, _, _) = CrearContexto();

        var resultado = await resolvedor.ResolverAsync(ComercioId, Comando(AccionIntencion.LISTAR_PRODUCTOS));

        Assert.Equal(EstadoResolucion.NO_APLICA, resultado.Estado);
    }

    [Fact]
    public async Task Resolver_CrearProducto_NoAplica()
    {
        var (resolvedor, _, _, _) = CrearContexto();

        var resultado = await resolvedor.ResolverAsync(ComercioId, Comando(AccionIntencion.CREAR_PRODUCTO, "SPRITE"));

        Assert.Equal(EstadoResolucion.NO_APLICA, resultado.Estado);
    }

    [Fact]
    public async Task Resolver_ProductoInexistente_NoEncontrado()
    {
        var (resolvedor, _, _, _) = CrearContexto();

        var resultado = await resolvedor.ResolverAsync(ComercioId, Comando(AccionIntencion.CONSULTAR_STOCK, "FANTA", "600ML"));

        Assert.Equal(EstadoResolucion.NO_ENCONTRADO, resultado.Estado);
        Assert.Contains("FANTA", resultado.Motivo);
    }

    [Fact]
    public async Task Resolver_UnaSolaPresentacion_Ok()
    {
        var (resolvedor, _, producto, pres1) = CrearContexto();

        var resultado = await resolvedor.ResolverAsync(ComercioId, Comando(AccionIntencion.CONSULTAR_STOCK, "COCA COLA"));

        Assert.Equal(EstadoResolucion.OK, resultado.Estado);
        Assert.Equal(pres1.Id, resultado.Coincidencia?.Presentacion.Id);
        Assert.Same(producto, resultado.Coincidencia?.Producto);
    }

    [Fact]
    public async Task Resolver_SinPresentacionesActivas_NoEncontrado()
    {
        var (resolvedor, productos, _, _) = CrearContexto();
        productos.Productos.Single().Presentaciones.Single().Desactivar();

        var resultado = await resolvedor.ResolverAsync(ComercioId, Comando(AccionIntencion.CONSULTAR_STOCK, "COCA COLA"));

        Assert.Equal(EstadoResolucion.NO_ENCONTRADO, resultado.Estado);
        Assert.Contains("no tiene presentaciones activas", resultado.Motivo);
    }

    [Fact]
    public async Task Resolver_PresentacionInexistente_NoEncontrado()
    {
        var (resolvedor, _, _, _) = CrearContexto(conSegundaPresentacion: true);

        var resultado = await resolvedor.ResolverAsync(ComercioId, Comando(AccionIntencion.CONSULTAR_STOCK, "COCA COLA", "2L"));

        Assert.Equal(EstadoResolucion.NO_ENCONTRADO, resultado.Estado);
        Assert.Contains("'2L'", resultado.Motivo);
    }

    [Fact]
    public async Task Resolver_MultiplesPresentaciones_Ambiguo()
    {
        var (resolvedor, _, _, _) = CrearContexto(conSegundaPresentacion: true);

        var resultado = await resolvedor.ResolverAsync(ComercioId, Comando(AccionIntencion.CONSULTAR_STOCK, "COCA COLA"));

        Assert.Equal(EstadoResolucion.AMBIGUO, resultado.Estado);
        Assert.Equal(2, resultado.Candidatos.Count);
        Assert.Null(resultado.Coincidencia);
    }

    [Fact]
    public async Task Resolver_MultiplesPresentaciones_PeroIndicada_Ok()
    {
        var (resolvedor, _, producto, pres1) = CrearContexto(conSegundaPresentacion: true);

        var resultado = await resolvedor.ResolverAsync(ComercioId, Comando(AccionIntencion.CONSULTAR_STOCK, "COCA COLA", "1.5L"));

        Assert.Equal(EstadoResolucion.OK, resultado.Estado);
        Assert.Equal(pres1.Id, resultado.Coincidencia?.Presentacion.Id);
    }

    [Fact]
    public async Task Buscar_PorNombreParcial_EncuentraPorContiene()
    {
        var (resolvedor, _, _, pres1) = CrearContexto();

        var resultado = await resolvedor.BuscarAsync(ComercioId, "COCA", null);

        Assert.True(resultado.ProductoEncontrado);
        Assert.Equal(pres1.Id, Assert.Single(resultado.Coincidencias).Presentacion.Id);
    }

    [Fact]
    public async Task Resolver_AmbiguedadNuncaEligeArbitrariamente()
    {
        var (resolvedor, _, _, _) = CrearContexto(conSegundaPresentacion: true, segundaPresentacion: "1L");

        var resultado = await resolvedor.ResolverAsync(ComercioId, Comando(AccionIntencion.CONSULTAR_PRECIO, "COCA COLA"));

        Assert.Equal(EstadoResolucion.AMBIGUO, resultado.Estado);
        Assert.Null(resultado.Coincidencia);
    }
}
