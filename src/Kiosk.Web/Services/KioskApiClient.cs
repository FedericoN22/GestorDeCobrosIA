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
