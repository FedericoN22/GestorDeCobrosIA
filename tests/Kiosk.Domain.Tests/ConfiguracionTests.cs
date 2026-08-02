using Kiosk.Domain.Configuracion;
using ConfiguracionEntidad = Kiosk.Domain.Configuracion.Configuracion;

namespace Kiosk.Domain.Tests;

public class ConfiguracionTests
{
    [Fact]
    public void Crear_AsignaClaveYValor()
    {
        var config = ConfiguracionEntidad.Crear(Guid.NewGuid(), " bot.nombre ", "Kiosco Bot");

        Assert.Equal("bot.nombre", config.Clave);
        Assert.Equal("Kiosco Bot", config.Valor);
    }

    [Fact]
    public void Crear_SinClave_LanzaError()
    {
        Assert.Throws<Exception>(() => ConfiguracionEntidad.Crear(Guid.NewGuid(), "  ", "valor"));
    }

    [Fact]
    public void CambiarValor_ActualizaValorYTimestamp()
    {
        var config = ConfiguracionEntidad.Crear(Guid.NewGuid(), "bot.nombre", "Kiosco Bot");
        config.CambiarValor("Bot Nuevo");

        Assert.Equal("Bot Nuevo", config.Valor);
    }
}
