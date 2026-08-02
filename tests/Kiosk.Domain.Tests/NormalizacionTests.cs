using Kiosk.Domain.Common;

namespace Kiosk.Domain.Tests;

public class NormalizacionTests
{
    [Theory]
    [InlineData("Coca Cola", "COCA COLA")]
    [InlineData("  pepsi  ", "PEPSI")]
    [InlineData("ÁGUA", "AGUA")]
    [InlineData("Ñandú", "NANDU")]
    [InlineData("François", "FRANCOIS")]
    public void Normalizar_EliminaAcentosMayusculasYEspacios(string entrada, string esperado)
    {
        Assert.Equal(esperado, Normalizacion.Normalizar(entrada));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalizar_ConVacio_DevuelveVacio(string? entrada)
    {
        Assert.Equal(string.Empty, Normalizacion.Normalizar(entrada!));
    }
}
