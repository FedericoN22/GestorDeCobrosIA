using System.Net.Http.Json;
using Kiosk.Application.Puertos.Integraciones;

namespace Kiosk.Ia;

public sealed class MetaMediaDownloader : IWhatsAppMediaDownloader
{
    private const string VersionApi = "v21.0";

    private readonly IHttpClientFactory _http;
    private readonly MetaOptions _options;

    public MetaMediaDownloader(IHttpClientFactory http, MetaOptions options)
    {
        _http = http;
        _options = options;
    }

    public async Task<byte[]?> DescargarAsync(string mediaId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mediaId) || _options.ModoSimulacion)
        {
            return null;
        }

        var client = _http.CreateClient(nameof(MetaMediaDownloader));
        var metadata = await client.GetFromJsonAsync<MediaMetadata>($"/{VersionApi}/{mediaId}", cancellationToken);

        if (metadata?.Url is null)
        {
            return null;
        }

        return await client.GetByteArrayAsync(metadata.Url, cancellationToken);
    }

    private sealed record MediaMetadata(string? Url, string? MimeType);
}
