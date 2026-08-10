using System.Text;
using System.Text.Json;
using Kiosk.Application.CasosUso.Whatsapp;
using Kiosk.Application.Puertos.Integraciones;
using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Usuarios;
using Kiosk.Ia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kiosk.Api.Controllers;

[ApiController]
[Route("api/whatsapp")]
public sealed class WhatsAppController : ControllerBase
{
    private readonly ServicioWhatsApp _servicio;
    private readonly IWhatsAppWhitelistRepository _whitelist;
    private readonly IWhatsAppMediaDownloader _mediaDownloader;
    private readonly ITranscriber _transcriber;
    private readonly IWhatsAppSender _sender;
    private readonly MetaOptions _meta;

    public WhatsAppController(
        ServicioWhatsApp servicio,
        IWhatsAppWhitelistRepository whitelist,
        IWhatsAppMediaDownloader mediaDownloader,
        ITranscriber transcriber,
        IWhatsAppSender sender,
        MetaOptions meta)
    {
        _servicio = servicio;
        _whitelist = whitelist;
        _mediaDownloader = mediaDownloader;
        _transcriber = transcriber;
        _sender = sender;
        _meta = meta;
    }

    [HttpGet("webhook")]
    [AllowAnonymous]
    public IActionResult Verificar(string hubMode, string hubVerifyToken, string hubChallenge)
    {
        if (string.Equals(hubMode, "subscribe", StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(_meta.VerifyToken)
            && string.Equals(hubVerifyToken, _meta.VerifyToken, StringComparison.Ordinal))
        {
            return Content(hubChallenge, "text/plain");
        }

        return Forbid();
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Recibir(CancellationToken cancellationToken)
    {
        using var lector = new StreamReader(Request.Body, Encoding.UTF8);
        var cuerpoCrudo = await lector.ReadToEndAsync(cancellationToken);

        if (!FirmaVerificacion.EsValida(
                Request.Headers["X-Hub-Signature-256"].ToString(),
                cuerpoCrudo,
                _meta.AppSecret))
        {
            return Unauthorized();
        }

        var payload = JsonSerializer.Deserialize<WebhookPayload>(cuerpoCrudo);
        if (payload?.Entry is null)
        {
            return Ok();
        }

        foreach (var mensaje in Mensajes(payload))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            await ProcesarMensajeAsync(mensaje, cancellationToken);
        }

        return Ok();
    }

    [HttpPost("simular")]
    [Authorize(Policy = Permisos.WhatsappOperar)]
    public async Task<ActionResult<SimularResponse>> Simular(SimularRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Numero) || string.IsNullOrWhiteSpace(request.Texto))
        {
            return BadRequest(new { error = "SIMULACION_INVALIDA", message = "Faltan 'numero' o 'texto'." });
        }

        var comercioId = ComercioId ?? await _whitelist.BuscarComercioActivoAsync(request.Numero, cancellationToken);
        if (comercioId is not Guid comercio)
        {
            return NotFound(new { error = "NUMERO_NO_AUTORIZADO", message = "El número no está en la whitelist del comercio." });
        }

        var respuesta = await _servicio.ProcesarMensajeAsync(comercio, request.Numero, request.Texto, false, cancellationToken);
        return Ok(new SimularResponse(respuesta));
    }

    private Guid? ComercioId =>
        Guid.TryParse(User.FindFirst("comercio_id")?.Value, out var id) ? id : null;

    private async Task ProcesarMensajeAsync(WebhookMessage mensaje, CancellationToken ct)
    {
        var numero = mensaje.From;
        if (string.IsNullOrWhiteSpace(numero) || string.IsNullOrWhiteSpace(mensaje.Id))
        {
            return;
        }

        var comercioId = await _whitelist.BuscarComercioActivoAsync(numero, ct);
        if (comercioId is not Guid comercio)
        {
            return;
        }

        if (string.Equals(mensaje.Type, "audio", StringComparison.OrdinalIgnoreCase))
        {
            await ProcesarAudioAsync(comercio, numero, mensaje.Audio, ct);
            return;
        }

        var texto = mensaje.Text?.Body;
        if (!string.IsNullOrWhiteSpace(texto))
        {
            await _servicio.ProcesarMensajeAsync(comercio, numero, texto, false, ct);
        }
    }

    private async Task ProcesarAudioAsync(Guid comercioId, string numero, WebhookAudio? audio, CancellationToken ct)
    {
        var mediaId = audio?.Id;
        if (string.IsNullOrWhiteSpace(mediaId))
        {
            return;
        }

        var bytes = await _mediaDownloader.DescargarAsync(mediaId, ct);
        if (bytes is null || bytes.Length == 0)
        {
            await _sender.EnviarAsync(numero, "No pude descargar tu audio. Escribime tu pedido por texto, por favor.", ct);
            return;
        }

        using var stream = new MemoryStream(bytes);
        var texto = await _transcriber.TranscribirAsync(stream, ExtensionDesdeMime(audio!.MimeType), ct);

        if (string.IsNullOrWhiteSpace(texto))
        {
            await _sender.EnviarAsync(numero, "No pude escuchar tu audio. Escribime tu pedido por texto, por favor.", ct);
            return;
        }

        await _servicio.ProcesarMensajeAsync(comercioId, numero, texto, true, ct);
    }

    private static IEnumerable<WebhookMessage> Mensajes(WebhookPayload payload)
    {
        if (payload.Entry is null)
        {
            yield break;
        }

        foreach (var entry in payload.Entry)
        {
            if (entry.Changes is null)
            {
                continue;
            }

            foreach (var change in entry.Changes)
            {
                if (change.Value?.Messages is null)
                {
                    continue;
                }

                foreach (var mensaje in change.Value.Messages)
                {
                    yield return mensaje;
                }
            }
        }
    }

    private static string ExtensionDesdeMime(string? mimeType) => mimeType?.ToLowerInvariant() switch
    {
        "audio/ogg" => "ogg",
        "audio/mpeg" or "audio/mp3" => "mp3",
        "audio/mp4" or "audio/x-m4a" => "m4a",
        "audio/wav" => "wav",
        "audio/webm" => "webm",
        _ => "ogg"
    };
}

public sealed record WebhookPayload(string? Object, IReadOnlyList<WebhookEntry>? Entry);

public sealed record WebhookEntry(string? Id, IReadOnlyList<WebhookChange>? Changes);

public sealed record WebhookChange(WebhookValue? Value, string? Field);

public sealed record WebhookValue(IReadOnlyList<WebhookMessage>? Messages);

public sealed record WebhookMessage(string? From, string? Id, string? Type, WebhookText? Text, WebhookAudio? Audio);

public sealed record WebhookText(string? Body);

public sealed record WebhookAudio(string? Id, string? MimeType);

public sealed record SimularRequest(string Numero, string Texto);

public sealed record SimularResponse(string Respuesta);
