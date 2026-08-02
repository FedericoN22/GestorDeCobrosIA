using Kiosk.Domain.Whatsapp;

namespace Kiosk.Domain.Tests;

public class WhatsappWhitelistTests
{
    [Fact]
    public void Autorizar_AsignaNumeroYActivo()
    {
        var whitelist = WhatsappWhitelist.Autorizar(Guid.NewGuid(), " +5491100000000 ");

        Assert.Equal("+5491100000000", whitelist.WhatsappNumero);
        Assert.True(whitelist.Activo);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Autorizar_ConNumeroVacio_LanzaError(string? numero)
    {
        AssertHelper.ThrowsDomain(
            "WHITELIST_NUMERO_REQUERIDO",
            () => WhatsappWhitelist.Autorizar(Guid.NewGuid(), numero!));
    }

    [Fact]
    public void Desactivar_PoneActivoEnFalse()
    {
        var whitelist = WhatsappWhitelist.Autorizar(Guid.NewGuid(), "+5491100000000");
        whitelist.Desactivar();

        Assert.False(whitelist.Activo);
    }
}
