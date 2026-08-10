using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Kiosk.Application.Puertos.Integraciones;

namespace Kiosk.Ia;

public sealed class MetaWhatsAppSender : IWhatsAppSender
{
    private const string VersionApi = "v21.0";

    private readonly IHttpClientFactory _http;
    private readonly MetaOptions _options;
    private readonly ILogger<MetaWhatsAppSender> _logger;

    public MetaWhatsAppSender(IHttpClientFactory http, MetaOptions options, ILogger<MetaWhatsAppSender> logger)
    {
        _http = http;
        _options = options;
        _logger = logger;
    }

    public async Task EnviarAsync(string whatsappNumero, string texto, CancellationToken cancellationToken = default)
    {
        if (_options.ModoSimulacion)
        {
            _logger.LogInformation("[SIMULACION] Mensaje a {Numero}: {Texto}", whatsappNumero, texto);
            return;
        }

        var payload = new
        {
            messaging_product = "whatsapp",
            to = whatsappNumero,
            text = new { body = texto }
        };

        var client = _http.CreateClient(nameof(MetaWhatsAppSender));
        var response = await client.PostAsJsonAsync($"/{VersionApi}/{_options.PhoneNumberId}/messages", payload, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var cuerpo = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Meta devolvió {(int)}: {Cuerpo}", (int)response.StatusCode, cuerpo);
        }

        response.EnsureSuccessStatusCode();
    }
}
