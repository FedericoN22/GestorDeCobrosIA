using Kiosk.Domain.Catalogos;

namespace Kiosk.Domain.Tests;

public class ProductoTests
{
    private static Producto CrearProducto()
        => Producto.Crear(Guid.NewGuid(), null, "Coca Cola");

    [Fact]
    public void Crear_NormalizaElNombre()
    {
        var producto = Producto.Crear(Guid.NewGuid(), null, "  Coca Cola ÁGUA  ");

        Assert.Equal("Coca Cola ÁGUA", producto.Nombre);
        Assert.Equal("COCA COLA AGUA", producto.NombreNormalizado);
        Assert.True(producto.Activo);
        Assert.Empty(producto.Presentaciones);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_ConNombreVacio_LanzaError(string? nombre)
    {
        AssertHelper.ThrowsDomain("PRODUCTO_NOMBRE_REQUERIDO", () => Producto.Crear(Guid.NewGuid(), null, nombre!));
    }

    [Fact]
    public void CambiarNombre_ActualizaYReNormaliza()
    {
        var producto = CrearProducto();
        producto.CambiarNombre("Pepsi");

        Assert.Equal("Pepsi", producto.Nombre);
        Assert.Equal("PEPSI", producto.NombreNormalizado);
    }

    [Fact]
    public void CambiarCategoria_ActualizaCategoria()
    {
        var producto = CrearProducto();
        var categoriaId = Guid.NewGuid();

        producto.CambiarCategoria(categoriaId);

        Assert.Equal(categoriaId, producto.CategoriaId);
    }

    [Fact]
    public void AgregarPresentacion_ConDatosValidos_AgregaPresentacion()
    {
        var producto = CrearProducto();
        var presentacion = producto.AgregarPresentacion("2.25L", 4200, 3500, "7790000000001");

        Assert.Single(producto.Presentaciones);
        Assert.Equal(producto.Id, presentacion.ProductoId);
        Assert.Equal(4200, presentacion.PrecioVentaCentavos);
        Assert.Equal("7790000000001", presentacion.CodigoBarras);
    }

    [Fact]
    public void AgregarPresentacion_NombreDuplicado_LanzaError()
    {
        var producto = CrearProducto();
        producto.AgregarPresentacion("2.25L", 4200);

        AssertHelper.ThrowsDomain("PRESENTACION_DUPLICADA", () => producto.AgregarPresentacion("2.25L", 4500));
    }

    [Fact]
    public void AgregarPresentacion_CodigoBarrasDuplicadoEntreActivas_LanzaError()
    {
        var producto = CrearProducto();
        producto.AgregarPresentacion("2.25L", 4200, null, "7790000000001");

        AssertHelper.ThrowsDomain(
            "CODIGO_BARRAS_DUPLICADO",
            () => producto.AgregarPresentacion("1.5L", 3500, null, "7790000000001"));
    }

    [Fact]
    public void AgregarPresentacion_CodigoBarrasDuplicadoEnDesactivada_NoLanzaError()
    {
        var producto = CrearProducto();
        var desactivada = producto.AgregarPresentacion("2.25L", 4200, null, "7790000000001");
        desactivada.Desactivar();

        var nueva = producto.AgregarPresentacion("1.5L", 3500, null, "7790000000001");

        Assert.Equal("7790000000001", nueva.CodigoBarras);
    }

    [Fact]
    public void Desactivar_PoneActivoEnFalse()
    {
        var producto = CrearProducto();
        producto.Desactivar();

        Assert.False(producto.Activo);
    }
}
