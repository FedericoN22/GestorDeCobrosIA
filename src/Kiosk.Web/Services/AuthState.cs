using System.Text.Json;
using Kiosk.Web.Models;
using Microsoft.JSInterop;

namespace Kiosk.Web.Services;

public sealed class AuthState
{
    private const string StorageKey = "kiosk.auth";

    private readonly IJSRuntime _js;

    public AuthState(IJSRuntime js)
    {
        _js = js;
    }

    public string? Token { get; private set; }

    public UsuarioResponse? Usuario { get; private set; }

    public bool IsAuthenticated => Token is not null && Usuario is not null;

    public bool IsInitialized { get; private set; }

    public event Action? OnChange;

    public bool TienePermiso(string permiso) => Usuario?.Permisos.Contains(permiso) ?? false;

    public async Task InicializarAsync()
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (string.IsNullOrEmpty(json))
            {
                IsInitialized = true;
                return;
            }

            var guardado = JsonSerializer.Deserialize<AuthGuardado>(json);
            if (guardado is not null && !string.IsNullOrEmpty(guardado.Token) && guardado.Usuario is not null)
            {
                Token = guardado.Token;
                Usuario = guardado.Usuario;
                OnChange?.Invoke();
            }
        }
        catch
        {
        }
        finally
        {
            IsInitialized = true;
        }
    }

    public async Task EstablecerAsync(string token, UsuarioResponse usuario)
    {
        Token = token;
        Usuario = usuario;
        try
        {
            var json = JsonSerializer.Serialize(new AuthGuardado(token, usuario));
            await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
        }
        catch
        {
        }
        OnChange?.Invoke();
    }

    public async Task LimpiarAsync()
    {
        Token = null;
        Usuario = null;
        try
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
        }
        catch
        {
        }
        OnChange?.Invoke();
    }

    private sealed record AuthGuardado(string Token, UsuarioResponse Usuario);
}
