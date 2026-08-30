using Kiosk.Application.CasosUso.Chat;
using Kiosk.Application.CasosUso.Whatsapp;
using Kiosk.Application.Puertos.Integraciones;
using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Common;

namespace Kiosk.Application.CasosUso.Whatsapp;

public sealed class ServicioWhatsApp
{
    private readonly IWhatsAppWhitelistRepository _whitelist;
    private readonly IConfiguracionRepository _config;
    private readonly IWhatsAppSender _sender;
    private readonly PipelineConversacional _pipeline;
    private readonly RateLimiterWhatsApp _rateLimiter;

    public ServicioWhatsApp(
        IWhatsAppWhitelistRepository whitelist,
        IConfiguracionRepository config,
        IWhatsAppSender sender,
        PipelineConversacional pipeline,
        RateLimiterWhatsApp rateLimiter)
    {
        _whitelist = whitelist;
        _config = config;
        _sender = sender;
        _pipeline = pipeline;
        _rateLimiter = rateLimiter;
    }

    public async Task<string> ProcesarMensajeAsync(
        Guid comercioId,
        Canal canal,
        string canalUsuarioId,
        string texto,
        bool fueAudio = false,
        CancellationToken cancellationToken = default)
    {
        if (!await _whitelist.EstaAutorizadoAsync(comercioId, canalUsuarioId, cancellationToken))
        {
            return await ResponderAsync(canalUsuarioId, RespuestasBot.NoAutorizado, cancellationToken);
        }

        var limite = await ObtenerEnteroAsync(
            comercioId, ClavesConfiguracion.BotLimiteMensajesPorMinuto, 10, cancellationToken);

        if (!_rateLimiter.Permitir(canalUsuarioId, limite, DateTime.UtcNow))
        {
            return await ResponderAsync(canalUsuarioId, RespuestasBot.LimiteExcedido, cancellationToken);
        }

        var respuesta = await _pipeline.ProcesarAsync(comercioId, canal, canalUsuarioId, texto, fueAudio, cancellationToken);
        return await ResponderAsync(canalUsuarioId, respuesta, cancellationToken);
    }

    private async Task<string> ResponderAsync(string canalUsuarioId, string texto, CancellationToken ct)
    {
        try
        {
            await _sender.EnviarAsync(canalUsuarioId, texto, ct);
        }
        catch
        {
        }

        return texto;
    }

    private async Task<string?> ObtenerTextoAsync(Guid comercioId, string clave, CancellationToken ct)
    {
        var config = await _config.GetAsync(comercioId, clave, ct);
        return config?.Valor;
    }

    private async Task<int> ObtenerEnteroAsync(Guid comercioId, string clave, int porDefecto, CancellationToken ct)
    {
        var valor = await ObtenerTextoAsync(comercioId, clave, ct);
        return int.TryParse(valor, out var n) ? n : porDefecto;
    }
}
