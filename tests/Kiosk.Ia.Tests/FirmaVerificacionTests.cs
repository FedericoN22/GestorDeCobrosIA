using Kiosk.Ia;

namespace Kiosk.Ia.Tests;

public class FirmaVerificacionTests
{
    private const string Secret = "mi-app-secret";
    private const string Cuerpo = """{"entry":[{"id":"123"}]}""";

    private static string Firma(string cuerpo)
        => FirmaVerificacion.CalcularSha256(Secret, cuerpo);

    [Fact]
    public void FirmaCorrecta_EsValida()
    {
        Assert.True(FirmaVerificacion.EsValida($"sha256={Firma(Cuerpo)}", Cuerpo, Secret));
    }

    [Fact]
    public void PrefijoEnMayuscula_EsValida()
    {
        Assert.True(FirmaVerificacion.EsValida($"SHA256={Firma(Cuerpo)}", Cuerpo, Secret));
    }

    [Fact]
    public void CuerpoModificado_NoEsValida()
    {
        Assert.False(FirmaVerificacion.EsValida($"sha256={Firma(Cuerpo)}", """{"entry":[]}""", Secret));
    }

    [Fact]
    public void SecretDistinto_NoEsValida()
    {
        Assert.False(FirmaVerificacion.EsValida($"sha256={Firma(Cuerpo)}", Cuerpo, "otro-secreto"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SinFirmaEnHeader_NoEsValida(string? firmaHeader)
    {
        Assert.False(FirmaVerificacion.EsValida(firmaHeader, Cuerpo, Secret));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SinSecretConfigurado_NoEsValida(string? appSecret)
    {
        Assert.False(FirmaVerificacion.EsValida($"sha256={Firma(Cuerpo)}", Cuerpo, appSecret));
    }

    [Fact]
    public void SinPrefijoSha256_NoEsValida()
    {
        Assert.False(FirmaVerificacion.EsValida($"hmac={Firma(Cuerpo)}", Cuerpo, Secret));
    }

    [Fact]
    public void HexEnMayuscula_NoEsValida()
    {
        Assert.False(FirmaVerificacion.EsValida($"sha256={Firma(Cuerpo).ToUpperInvariant()}", Cuerpo, Secret));
    }
}
