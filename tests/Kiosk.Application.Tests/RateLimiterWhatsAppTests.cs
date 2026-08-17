using Kiosk.Application.CasosUso.Whatsapp;

namespace Kiosk.Application.Tests;

public class RateLimiterWhatsAppTests
{
    private static readonly DateTime Base = new(2026, 8, 11, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void DentroDelLimite_Permite()
    {
        var limiter = new RateLimiterWhatsApp();

        Assert.True(limiter.Permitir("5491100000000", 3, Base));
        Assert.True(limiter.Permitir("5491100000000", 3, Base.AddSeconds(10)));
        Assert.True(limiter.Permitir("5491100000000", 3, Base.AddSeconds(20)));
    }

    [Fact]
    public void ExcedeElLimite_Rechaza()
    {
        var limiter = new RateLimiterWhatsApp();

        Assert.True(limiter.Permitir("5491100000000", 2, Base));
        Assert.True(limiter.Permitir("5491100000000", 2, Base.AddSeconds(5)));

        Assert.False(limiter.Permitir("5491100000000", 2, Base.AddSeconds(10)));
    }

    [Fact]
    public void VentanaExpirada_LiberaElLugar()
    {
        var limiter = new RateLimiterWhatsApp();

        Assert.True(limiter.Permitir("5491100000000", 1, Base));
        Assert.False(limiter.Permitir("5491100000000", 1, Base.AddSeconds(30)));

        Assert.True(limiter.Permitir("5491100000000", 1, Base.AddSeconds(61)));
    }

    [Fact]
    public void LimiteCero_RechazaSiempre()
    {
        var limiter = new RateLimiterWhatsApp();

        Assert.False(limiter.Permitir("5491100000000", 0, Base));
    }

    [Fact]
    public void NumerosDistintos_TienenVentanasIndependientes()
    {
        var limiter = new RateLimiterWhatsApp();

        Assert.True(limiter.Permitir("5491100000001", 1, Base));
        Assert.False(limiter.Permitir("5491100000001", 1, Base.AddSeconds(5)));

        Assert.True(limiter.Permitir("5491100000002", 1, Base.AddSeconds(5)));
    }
}
