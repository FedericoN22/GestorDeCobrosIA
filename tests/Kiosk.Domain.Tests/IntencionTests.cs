using Kiosk.Domain.Whatsapp;

namespace Kiosk.Domain.Tests;

public class IntencionTests
{
    private static readonly Guid ComercioId = Guid.NewGuid();

    private static Intencion Recibir(string texto = "¿Cuánto stock hay de Coca Cola?")
        => Intencion.Recibir(ComercioId, "+5491100000000", texto);

    [Fact]
    public void Recibir_AsignaPropiedadesIniciales()
    {
        var intencion = Recibir();

        Assert.Equal(EstadoIntencion.RECIBIDA, intencion.Estado);
        Assert.Equal("+5491100000000", intencion.WhatsappNumero);
        Assert.False(intencion.FueAudio);
        Assert.Null(intencion.StructuredCommandJson);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Recibir_ConNumeroVacio_LanzaError(string? numero)
    {
        AssertHelper.ThrowsDomain(
            "INTENCION_NUMERO_REQUERIDO",
            () => Intencion.Recibir(ComercioId, numero!, "hola"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Recibir_ConTextoVacio_LanzaError(string? texto)
    {
        AssertHelper.ThrowsDomain(
            "INTENCION_TEXTO_REQUERIDO",
            () => Intencion.Recibir(ComercioId, "+5491100000000", texto!));
    }

    [Fact]
    public void MarcarParseada_CambiaEstadoYGuardarComando()
    {
        var intencion = Recibir();
        intencion.MarcarParseada("{\"accion\":\"CONSULTAR_STOCK\"}");

        Assert.Equal(EstadoIntencion.PARSEADA, intencion.Estado);
        Assert.Equal("{\"accion\":\"CONSULTAR_STOCK\"}", intencion.StructuredCommandJson);
    }

    [Fact]
    public void MarcarParseada_ConComandoVacio_LanzaError()
    {
        var intencion = Recibir();

        AssertHelper.ThrowsDomain("INTENCION_COMANDO_REQUERIDO", () => intencion.MarcarParseada("  "));
    }

    [Fact]
    public void PedirConfirmacion_SoloDesdeParseada()
    {
        var intencion = Recibir();

        AssertHelper.ThrowsDomain(
            "INTENCION_ESTADO_INVALIDO",
            () => intencion.PedirConfirmacion(DateTime.UtcNow.AddMinutes(2)));
    }

    [Fact]
    public void PedirConfirmacion_DesdeParseada_CambiaEstadoYExpira()
    {
        var intencion = Recibir();
        intencion.MarcarParseada("{\"accion\":\"MODIFICAR_PRECIO\"}");
        var expira = DateTime.UtcNow.AddMinutes(2);

        intencion.PedirConfirmacion(expira);

        Assert.Equal(EstadoIntencion.ESPERANDO_CONFIRMACION, intencion.Estado);
        Assert.Equal(expira, intencion.ExpiraEn);
    }

    [Fact]
    public void PedirAclaracion_SoloDesdeParseada()
    {
        var intencion = Recibir();

        AssertHelper.ThrowsDomain(
            "INTENCION_ESTADO_INVALIDO",
            () => intencion.PedirAclaracion("Faltan campos"));
    }

    [Fact]
    public void PedirAclaracion_CambiaEstadoYGuardaDecision()
    {
        var intencion = Recibir();
        intencion.MarcarParseada("{\"accion\":\"AGREGAR_STOCK\"}");

        intencion.PedirAclaracion("¿Qué presentación? ¿600ml o 2.25L?");

        Assert.Equal(EstadoIntencion.ACLARACION, intencion.Estado);
        Assert.Equal("¿Qué presentación? ¿600ml o 2.25L?", intencion.Decision);
    }

    [Fact]
    public void Ejecutar_DesdeEsperandoConfirmacion_CambiaEstado()
    {
        var intencion = Recibir();
        intencion.MarcarParseada("{\"accion\":\"MODIFICAR_PRECIO\"}");
        intencion.PedirConfirmacion(DateTime.UtcNow.AddMinutes(2));

        intencion.Ejecutar("{\"precio\":4500}");

        Assert.Equal(EstadoIntencion.EJECUTADA, intencion.Estado);
        Assert.Equal("{\"precio\":4500}", intencion.ResultadoJson);
        Assert.Null(intencion.ExpiraEn);
    }

    [Fact]
    public void Ejecutar_DesdeRecibida_LanzaError()
    {
        var intencion = Recibir();

        AssertHelper.ThrowsDomain(
            "INTENCION_ESTADO_INVALIDO",
            () => intencion.Ejecutar("{}"));
    }

    [Fact]
    public void Cancelar_PoneEstadoCancelada()
    {
        var intencion = Recibir();
        intencion.MarcarParseada("{\"accion\":\"MODIFICAR_PRECIO\"}");
        intencion.PedirConfirmacion(DateTime.UtcNow.AddMinutes(2));

        intencion.Cancelar();

        Assert.Equal(EstadoIntencion.CANCELADA, intencion.Estado);
        Assert.Null(intencion.ExpiraEn);
    }

    [Fact]
    public void Rechazar_PoneEstadoRechazada()
    {
        var intencion = Recibir();
        intencion.Rechazar("No interpretable");

        Assert.Equal(EstadoIntencion.RECHAZADA, intencion.Estado);
        Assert.Equal("No interpretable", intencion.Decision);
    }

    [Fact]
    public void MarcarError_PoneEstadoError()
    {
        var intencion = Recibir();
        intencion.MarcarError("Falló la ejecución");

        Assert.Equal(EstadoIntencion.ERROR, intencion.Estado);
    }

    [Fact]
    public void ConfirmacionExpirada_CuandoNoEsperaConfirmacion_EsFalso()
    {
        var intencion = Recibir();
        intencion.MarcarParseada("{}");

        Assert.False(intencion.ConfirmacionExpirada(DateTime.UtcNow.AddMinutes(10)));
    }

    [Fact]
    public void ConfirmacionExpirada_CuandoVencida_EsVerdadero()
    {
        var intencion = Recibir();
        intencion.MarcarParseada("{}");
        intencion.PedirConfirmacion(DateTime.UtcNow.AddMinutes(2));

        Assert.True(intencion.ConfirmacionExpirada(DateTime.UtcNow.AddMinutes(3)));
        Assert.False(intencion.ConfirmacionExpirada(DateTime.UtcNow.AddMinutes(1)));
    }
}
