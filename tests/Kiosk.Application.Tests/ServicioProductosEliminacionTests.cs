using Kiosk.Application.CasosUso.Catalogos;
using Kiosk.Application.Tests.TestDoubles;
using Kiosk.Domain.Catalogos;
using Kiosk.Domain.Common;

namespace Kiosk.Application.Tests;

public class ServicioProductosEliminacionTests
{
    private static readonly Guid ComercioId = Guid.NewGuid();

    private sealed class Contexto
    {
        public Contexto()
        {
            Uow = new FakeUnitOfWork();
            Servicio = new ServicioProductos(Productos, Categorias, Auditoria, Uow);
        }

        public FakeProductRepository Productos { get; } = new();
        public FakeCategoriaRepository Categorias { get; } = new();
        public FakeAuditoriaRepository Auditoria { get; } = new();
        public FakeUnitOfWork Uow { get; }
        public ServicioProductos Servicio { get; }
    }

    private static (Producto Producto, Presentacion Presentacion1, Presentacion Presentacion2) SeedProductoConPresentaciones(Contexto ctx)
    {
        var producto = Producto.Crear(ComercioId, null, "Coca Cola");
        var pres1 = producto.AgregarPresentacion("1.5L", 3500);
        var pres2 = producto.AgregarPresentacion("2.25L", 5000);
        ctx.Productos.Seed(producto);
        return (producto, pres1, pres2);
    }

    [Fact]
    public async Task EliminarProducto_EliminaProductoYPresentaciones()
    {
        var ctx = new Contexto();
        var (producto, pres1, pres2) = SeedProductoConPresentaciones(ctx);

        var resultado = await ctx.Servicio.EliminarProductoAsync(
            new EliminarProductoCommand(ComercioId, producto.Id, "test", Canal.WEB));

        Assert.True(resultado.IsSuccess);
        Assert.DoesNotContain(ctx.Productos.Productos, p => p.Id == producto.Id);
    }

    [Fact]
    public async Task EliminarProducto_NoAfectaAOtrosProductos()
    {
        var ctx = new Contexto();
        var (producto1, _, _) = SeedProductoConPresentaciones(ctx);
        var producto2 = Producto.Crear(ComercioId, null, "Pepsi");
        ctx.Productos.Seed(producto2);

        var resultado = await ctx.Servicio.EliminarProductoAsync(
            new EliminarProductoCommand(ComercioId, producto1.Id, "test", Canal.WEB));

        Assert.True(resultado.IsSuccess);
        Assert.Contains(ctx.Productos.Productos, p => p.Id == producto2.Id);
    }

    [Fact]
    public async Task EliminarProducto_NoExiste_RetornaError()
    {
        var ctx = new Contexto();

        var resultado = await ctx.Servicio.EliminarProductoAsync(
            new EliminarProductoCommand(ComercioId, Guid.NewGuid(), "test", Canal.WEB));

        Assert.False(resultado.IsSuccess);
        Assert.Equal("PRODUCTO_NO_ENCONTRADO", resultado.Error!.Code);
    }

    [Fact]
    public async Task EliminarPresentacion_EliminaSoloEsaPresentacion()
    {
        var ctx = new Contexto();
        var (producto, pres1, pres2) = SeedProductoConPresentaciones(ctx);

        var resultado = await ctx.Servicio.EliminarPresentacionAsync(
            new EliminarPresentacionCommand(ComercioId, pres1.Id, "test", Canal.WEB));

        Assert.True(resultado.IsSuccess);
        Assert.Contains(ctx.Productos.Productos, p => p.Id == producto.Id);
    }

    [Fact]
    public async Task EliminarPresentacion_NoExiste_RetornaError()
    {
        var ctx = new Contexto();

        var resultado = await ctx.Servicio.EliminarPresentacionAsync(
            new EliminarPresentacionCommand(ComercioId, Guid.NewGuid(), "test", Canal.WEB));

        Assert.False(resultado.IsSuccess);
        Assert.Equal("PRESENTACION_NO_ENCONTRADA", resultado.Error!.Code);
    }
}
