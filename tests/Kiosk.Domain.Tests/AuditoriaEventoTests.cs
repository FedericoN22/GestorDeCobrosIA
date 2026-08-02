using Kiosk.Domain.Auditoria;
using Kiosk.Domain.Common;

namespace Kiosk.Domain.Tests;

public class AuditoriaEventoTests
{
    [Fact]
    public void Registrar_AsignaPropiedades()
    {
        var intencionId = Guid.NewGuid();
        var evento = AuditoriaEvento.Registrar(
            Guid.NewGuid(),
            Canal.WHATSAPP,
            "whatsapp:+5491100000000",
            "STOCK.CARGAR",
            "{\"cantidad\":12}",
            intencionId);

        Assert.Equal(Canal.WHATSAPP, evento.Canal);
        Assert.Equal("whatsapp:+5491100000000", evento.Actor);
        Assert.Equal("STOCK.CARGAR", evento.Tipo);
        Assert.Equal(intencionId, evento.IntencionId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Registrar_SinActor_LanzaError(string? actor)
    {
        AssertHelper.ThrowsDomain(
            "AUDITORIA_ACTOR_REQUERIDO",
            () => AuditoriaEvento.Registrar(Guid.NewGuid(), Canal.POS, actor!, "VENTA.CREAR"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Registrar_SinTipo_LanzaError(string? tipo)
    {
        AssertHelper.ThrowsDomain(
            "AUDITORIA_TIPO_REQUERIDO",
            () => AuditoriaEvento.Registrar(Guid.NewGuid(), Canal.POS, "admin", tipo!));
    }
}
