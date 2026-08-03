using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Kiosk.Pos.Models;

namespace Kiosk.Pos.Services;

public sealed class ResultadoApi<T>
{
    public bool Ok { get; init; }
    public T? Valor { get; init; }
    public string? Error { get; init; }
    public string? Mensaje { get; init; }

    public static ResultadoApi<T> Exito(T valor) => new() { Ok = true, Valor = valor };
    public static ResultadoApi<T> Fracaso(string? error, string? mensaje) => new() { Ok = false, Error = error, Mensaje = mensaje };
}

public sealed class ApiClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public ApiClient(string baseUrl, int timeoutSegundos = 15)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _http = new HttpClient { BaseAddress = new Uri(_baseUrl), Timeout = TimeSpan.FromSeconds(timeoutSegundos) };
    }

    public bool Online { get; private set; } = true;

    private void Marcar(bool exito)
    {
        var cambio = Online != exito;
        Online = exito;
        if (cambio)
        {
            EstadoConexion?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? EstadoConexion;

    private async Task<ResultadoApi<T>> PostAsync<T>(string ruta, object cuerpo, string? token = null, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, ruta)
            {
                Content = JsonContent.Create(cuerpo)
            };
            if (token is not null)
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            using var respuesta = await _http.SendAsync(request, ct).ConfigureAwait(false);
            Marcar(true);
            if (!respuesta.IsSuccessStatusCode)
            {
                return await LeerError<T>(respuesta, ct).ConfigureAwait(false);
            }

            var valor = await respuesta.Content.ReadFromJsonAsync<T>(cancellationToken: ct).ConfigureAwait(false);
            return ResultadoApi<T>.Exito(valor!);
        }
        catch (HttpRequestException ex)
        {
            Marcar(false);
            return ResultadoApi<T>.Fracaso("CONEXION", $"Sin conexión con el servidor: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            Marcar(false);
            return ResultadoApi<T>.Fracaso("TIMEOUT", "El servidor tardó demasiado en responder.");
        }
        catch (JsonException)
        {
            Marcar(true);
            return ResultadoApi<T>.Fracaso("RESPUESTA_INVALIDA", "El servidor devolvió una respuesta inválida.");
        }
    }

    private async Task<ResultadoApi<T>> GetAsync<T>(string ruta, string? token = null, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ruta);
            if (token is not null)
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            using var respuesta = await _http.SendAsync(request, ct).ConfigureAwait(false);
            Marcar(true);
            if (!respuesta.IsSuccessStatusCode)
            {
                return await LeerError<T>(respuesta, ct).ConfigureAwait(false);
            }

            var valor = await respuesta.Content.ReadFromJsonAsync<T>(cancellationToken: ct).ConfigureAwait(false);
            return ResultadoApi<T>.Exito(valor!);
        }
        catch (HttpRequestException ex)
        {
            Marcar(false);
            return ResultadoApi<T>.Fracaso("CONEXION", $"Sin conexión con el servidor: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            Marcar(false);
            return ResultadoApi<T>.Fracaso("TIMEOUT", "El servidor tardó demasiado en responder.");
        }
        catch (JsonException)
        {
            Marcar(true);
            return ResultadoApi<T>.Fracaso("RESPUESTA_INVALIDA", "El servidor devolvió una respuesta inválida.");
        }
    }

    private static async Task<ResultadoApi<T>> LeerError<T>(HttpResponseMessage respuesta, CancellationToken ct)
    {
        try
        {
            var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct).ConfigureAwait(false);
            if (cuerpo.ValueKind == JsonValueKind.Object)
            {
                var error = cuerpo.TryGetProperty("error", out var e) ? e.GetString() : null;
                var mensaje = cuerpo.TryGetProperty("message", out var m) ? m.GetString() : null;
                return ResultadoApi<T>.Fracaso(error, mensaje ?? $"El servidor respondió {(int)respuesta.StatusCode}.");
            }
        }
        catch (JsonException)
        {
        }

        return ResultadoApi<T>.Fracaso(null, $"El servidor respondió {(int)respuesta.StatusCode}.");
    }

    public Task<ResultadoApi<LoginResponseDto>> LoginAsync(string username, string password, CancellationToken ct = default)
        => PostAsync<LoginResponseDto>("/api/auth/login", new { username, password }, null, ct);

    public Task<ResultadoApi<CajaResponseDto?>> ObtenerCajaActivaAsync(string token, CancellationToken ct = default)
        => GetAsync<CajaResponseDto?>("/api/cajas/activa", token, ct);

    public Task<ResultadoApi<ProcesarBatchResultDto>> ProcesarBatchAsync(string token, IReadOnlyList<OperacionBatchDto> operaciones, CancellationToken ct = default)
        => PostAsync<ProcesarBatchResultDto>("/api/sync/batch", new { operaciones }, token, ct);

    public Task<ResultadoApi<EstadoSyncDto>> ObtenerEstadoAsync(string token, DateTime? cursor = null, CancellationToken ct = default)
    {
        var ruta = "/api/sync/state";
        if (cursor.HasValue)
        {
            ruta += $"?cursor={Uri.EscapeDataString(cursor.Value.ToString("O"))}";
        }

        return GetAsync<EstadoSyncDto>(ruta, token, ct);
    }

    public Task<ResultadoApi<ConfirmarSyncDto>> ConfirmarAsync(string token, IReadOnlyList<Guid> operationIds, CancellationToken ct = default)
        => PostAsync<ConfirmarSyncDto>("/api/sync/ack", new { operationIds }, token, ct);
}
