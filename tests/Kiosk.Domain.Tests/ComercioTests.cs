using Kiosk.Domain.Comercios;

namespace Kiosk.Domain.Tests;

public class ComercioTests
{
    [Fact]
    public void Crear_ConNombreValido_AsignaPropiedades()
    {
        var comercio = Comercio.Crear("  Kiosco Don Pepe  ");

        Assert.Equal("Kiosco Don Pepe", comercio.Nombre);
        Assert.NotEqual(Guid.Empty, comercio.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_ConNombreVacio_LanzaError(string? nombre)
    {
        AssertHelper.ThrowsDomain("COMERCIO_NOMBRE_REQUERIDO", () => Comercio.Crear(nombre!));
    }

    [Fact]
    public void CambiarNombre_ActualizaNombre()
    {
        var comercio = Comercio.Crear("Kiosco A");
        comercio.CambiarNombre("Kiosco B");

        Assert.Equal("Kiosco B", comercio.Nombre);
    }

    [Fact]
    public void CambiarNombre_ConNombreVacio_LanzaError()
    {
        var comercio = Comercio.Crear("Kiosco A");

        AssertHelper.ThrowsDomain("COMERCIO_NOMBRE_REQUERIDO", () => comercio.CambiarNombre("  "));
    }
}
