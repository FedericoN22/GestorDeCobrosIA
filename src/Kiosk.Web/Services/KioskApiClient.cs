using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Kiosk.Web.Models;

namespace Kiosk.Web.Services;

public sealed class ApiException : Exception
{
    public ApiException(string message, string? codigo, bool esAutenticacion)
        : base(message)
    {
        Codigo = codigo;
        EsAutenticacion = esAutenticacion;
    }

    public string? Codigo { get; }

    public bool EsAutenticacion { get; }
}

public sealed class KioskApiClient
{
    private readonly HttpClient _http;
    private readonly AuthState _auth;

    public KioskApiClient(HttpClient http, AuthState auth)
    {
        _http = http;
        _auth = auth;
    }

    public async Task<LoginResponse> LoginAsync(string username, string password, CancellationToken cancellationToken)
    {
        using var respuesta = await _http.PostAsJsonAsync("api/auth/login", new LoginRequest(username, password), cancellationToken);
        return await LeerAsync<LoginResponse>(respuesta, cancellationToken);
    }

    public async Task<IReadOnlyList<CategoriaResponse>> GetCategoriasAsync(CancellationToken cancellationToken)
    {
        using var respuesta = await EnviarAsync(() => _http.GetAsync("api/categorias", cancellationToken));
        return await LeerAsync<List<CategoriaResponse>>(respuesta, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductoResponse>> GetProductosAsync(CancellationToken cancellationToken)
    {
        using var respuesta = await EnviarAsync(() => _http.GetAsync("api/productos", cancellationToken));
        return await LeerAsync<List<ProductoResponse>>(respuesta, cancellationToken);
    }

    public async Task<CrearProductoResponse> CrearProductoAsync(string nombre, Guid? categoriaId, CancellationToken cancellationToken)
    {
        using var respuesta = await EnviarAsync(() => _http.PostAsJsonAsync("api/productos", new CrearProductoRequest(nombre, categoriaId), cancellationToken));
        return await LeerAsync<CrearProductoResponse>(respuesta, cancellationToken);
    }

    public async Task<PresentacionResponse> AgregarPresentacionAsync(Guid productoId, AgregarPresentacionRequest request, CancellationToken cancellationToken)
    {
        using var respuesta = await EnviarAsync(() => _http.PostAsJsonAsync($"api/productos/{productoId}/presentaciones", request, cancellationToken));
        return await LeerAsync<PresentacionResponse>(respuesta, cancellationToken);
    }

    public async Task<StockActualResponse> EntradaStockAsync(Guid presentacionId, int cantidad, int? precioCostoCentavos, CancellationToken cancellationToken)
    {
        using var respuesta = await EnviarAsync(() => _http.PostAsJsonAsync(
            "api/stock/entrada",
            new EntradaStockRequest(presentacionId, cantidad, precioCostoCentavos),
            cancellationToken));
        return await LeerAsync<StockActualResponse>(respuesta, cancellationToken);
    }

    public Uri BaseAddress => _http.BaseAddress!;

    public string UrlCsv(string rutaRelativa)
    {
        var ruta = rutaRelativa.TrimStart('/');
        return $"{_http.BaseAddress}{ruta}";
    }

    public async Task<ReporteVentasResponse> GetReporteVentasAsync(DateTime desde, DateTime hasta, CancellationToken cancellationToken)
    {
        using var respuesta = await EnviarAsync(() => _http.GetAsync($"api/reportes/ventas?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}", cancellationToken));
        return await LeerAsync<ReporteVentasResponse>(respuesta, cancellationToken);
    }

    public async Task<IReadOnlyList<CierreCajaReporte>> GetCierresAsync(Guid? usuarioId, DateTime desde, DateTime hasta, bool soloDiferencias, CancellationToken cancellationToken)
    {
        using var respuesta = await EnviarAsync(() => _http.GetAsync($"api/reportes/cierres?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}&soloDiferencias={soloDiferencias}", cancellationToken));
        return await LeerAsync<List<CierreCajaReporte>>(respuesta, cancellationToken);
    }

    public async Task<IReadOnlyList<MovimientoStockReporte>> GetMovimientosAsync(Guid? presentacionId, int? tipo, int? origen, Guid? usuarioId, DateTime desde, DateTime hasta, CancellationToken cancellationToken)
    {
        var consulta = $"api/reportes/movimientos?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}";
        if (presentacionId.HasValue) consulta += $"&presentacionId={presentacionId}";
        if (tipo.HasValue) consulta += $"&tipo={tipo}";
        if (origen.HasValue) consulta += $"&origen={origen}";
        if (usuarioId.HasValue) consulta += $"&usuarioId={usuarioId}";

        using var respuesta = await EnviarAsync(() => _http.GetAsync(consulta, cancellationToken));
        return await LeerAsync<List<MovimientoStockReporte>>(respuesta, cancellationToken);
    }

    public async Task<ReporteGananciasResponse> GetGananciasAsync(DateTime desde, DateTime hasta, CancellationToken cancellationToken)
    {
        using var respuesta = await EnviarAsync(() => _http.GetAsync($"api/reportes/ganancias?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}", cancellationToken));
        return await LeerAsync<ReporteGananciasResponse>(respuesta, cancellationToken);
    }

    public async Task<ReporteRankingResponse> GetRankingAsync(DateTime desde, DateTime hasta, int top, CancellationToken cancellationToken)
    {
        using var respuesta = await EnviarAsync(() => _http.GetAsync($"api/reportes/ranking?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}&top={top}", cancellationToken));
        return await LeerAsync<ReporteRankingResponse>(respuesta, cancellationToken);
    }

    public async Task<IReadOnlyList<AuditoriaEventoReporte>> GetAuditoriaAsync(int? canal, string? actor, string? tipo, DateTime desde, DateTime hasta, CancellationToken cancellationToken)
    {
        var consulta = $"api/reportes/auditoria?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}";
        if (canal.HasValue) consulta += $"&canal={canal}";
        if (!string.IsNullOrWhiteSpace(actor)) consulta += $"&actor={Uri.EscapeDataString(actor)}";
        if (!string.IsNullOrWhiteSpace(tipo)) consulta += $"&tipo={Uri.EscapeDataString(tipo)}";

        using var respuesta = await EnviarAsync(() => _http.GetAsync(consulta, cancellationToken));
        return await LeerAsync<List<AuditoriaEventoReporte>>(respuesta, cancellationToken);
    }

    public async Task<IReadOnlyList<WhatsappWhitelistResponse>> GetWhatsappWhitelistAsync(CancellationToken cancellationToken)
    {
        using var respuesta = await EnviarAsync(() => _http.GetAsync("api/whatsapp/whitelist", cancellationToken));
        return await LeerAsync<List<WhatsappWhitelistResponse>>(respuesta, cancellationToken);
    }

    public async Task<WhatsappWhitelistResponse> AgregarWhatsappWhitelistAsync(string whatsappNumero, CancellationToken cancellationToken)
    {
        using var respuesta = await EnviarAsync(() => _http.PostAsJsonAsync(
            "api/whatsapp/whitelist",
            new AgregarWhatsappWhitelistRequest(whatsappNumero),
            cancellationToken));
        return await LeerAsync<WhatsappWhitelistResponse>(respuesta, cancellationToken);
    }

    public async Task<WhatsappWhitelistResponse> QuitarWhatsappWhitelistAsync(Guid id, CancellationToken cancellationToken)
    {
        using var respuesta = await EnviarAsync(() => _http.DeleteAsync($"api/whatsapp/whitelist/{id}", cancellationToken));
        return await LeerAsync<WhatsappWhitelistResponse>(respuesta, cancellationToken);
    }

    public async Task<ConfiguracionBotResponse> GetConfiguracionBotAsync(CancellationToken cancellationToken)
    {
        using var respuesta = await EnviarAsync(() => _http.GetAsync("api/whatsapp/config/bot", cancellationToken));
        return await LeerAsync<ConfiguracionBotResponse>(respuesta, cancellationToken);
    }

    public async Task<ConfiguracionBotResponse> GuardarConfiguracionBotAsync(GuardarConfiguracionBotRequest request, CancellationToken cancellationToken)
    {
        using var respuesta = await EnviarAsync(() => _http.PutAsJsonAsync("api/whatsapp/config/bot", request, cancellationToken));
        return await LeerAsync<ConfiguracionBotResponse>(respuesta, cancellationToken);
    }

    private async Task<HttpResponseMessage> EnviarAsync(Func<Task<HttpResponseMessage>> enviar)
    {
        _http.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(_auth.Token)
            ? null
            : new AuthenticationHeaderValue("Bearer", _auth.Token);
        return await enviar();
    }

    private static async Task<T> LeerAsync<T>(HttpResponseMessage respuesta, CancellationToken cancellationToken)
    {
        if (respuesta.IsSuccessStatusCode)
        {
            return (await respuesta.Content.ReadFromJsonAsync<T>(cancellationToken))!;
        }

        var error = await TryLeerErrorAsync(respuesta, cancellationToken);
        var esAutenticacion = respuesta.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;
        throw new ApiException(
            error?.Message ?? $"La API respondió {(int)respuesta.StatusCode}.",
            error?.Error,
            esAutenticacion);
    }

    private static async Task<ErrorResponse?> TryLeerErrorAsync(HttpResponseMessage respuesta, CancellationToken cancellationToken)
    {
        try
        {
            return await respuesta.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
