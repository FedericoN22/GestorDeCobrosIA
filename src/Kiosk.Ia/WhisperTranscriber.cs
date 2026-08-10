using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Kiosk.Application.Puertos.Integraciones;

namespace Kiosk.Ia;

public sealed class WhisperTranscriber : ITranscriber
{
    private readonly IHttpClientFactory _http;
    private readonly OpenAiOptions _options;

    public WhisperTranscriber(IHttpClientFactory http, OpenAiOptions options)
    {
        _http = http;
        _options = options;
    }

    public async Task<string> TranscribirAsync(Stream audio, string extension, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return string.Empty;
        }

        var nombreArchivo = $"audio.{ExtensionValida(extension)}";

        using var form = new MultipartFormDataContent();
        using var contenidoAudio = new StreamContent(audio);
        form.Add(contenidoAudio, "file", nombreArchivo);
        form.Add(new StringContent(_options.ModeloWhisper), "model");
        form.Add(new StringContent("es"), "language");

        var client = _http.CreateClient(nameof(WhisperTranscriber));
        var response = await client.PostAsync("/v1/audio/transcriptions", form, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var cuerpo = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Whisper devolvió {(int)response.StatusCode}: {cuerpo}");
        }

        var resultado = await response.Content.ReadFromJsonAsync<TranscripcionResponse>(cancellationToken);
        return resultado?.Text?.Trim() ?? string.Empty;
    }

    private static string ExtensionValida(string extension)
    {
        var permitidas = new[] { "ogg", "mp3", "m4a", "wav", "mp4", "webm", "oga", "flac" };
        var limpia = string.IsNullOrWhiteSpace(extension) ? "ogg" : extension.Trim().TrimStart('.').ToLowerInvariant();
        return permitidas.Contains(limpia) ? limpia : "ogg";
    }

    private sealed record TranscripcionResponse([property: JsonPropertyName("text")] string? Text);
}
