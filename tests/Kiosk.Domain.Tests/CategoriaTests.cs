using Kiosk.Domain.Catalogos;

namespace Kiosk.Domain.Tests;

public class CategoriaTests
{
    [Fact]
    public void Crear_ConNombreValido_AsignaPropiedades()
    {
        var categoria = Categoria.Crear(Guid.NewGuid(), "  Bebidas  ");

        Assert.Equal("Bebidas", categoria.Nombre);
        Assert.True(categoria.Activa);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_ConNombreVacio_LanzaError(string? nombre)
    {
        AssertHelper.ThrowsDomain("CATEGORIA_NOMBRE_REQUERIDO", () => Categoria.Crear(Guid.NewGuid(), nombre!));
    }

    [Fact]
    public void CambiarNombre_ActualizaNombre()
    {
        var categoria = Categoria.Crear(Guid.NewGuid(), "Bebidas");
        categoria.CambiarNombre("Aguas");

        Assert.Equal("Aguas", categoria.Nombre);
    }

    [Fact]
    public void Desactivar_PoneActivaEnFalse()
    {
        var categoria = Categoria.Crear(Guid.NewGuid(), "Bebidas");
        categoria.Desactivar();

        Assert.False(categoria.Activa);
    }
}
