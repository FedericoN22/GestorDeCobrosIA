using Kiosk.Web.Models;

namespace Kiosk.Web.Services;

public sealed class AuthState
{
    public string? Token { get; private set; }

    public UsuarioResponse? Usuario { get; private set; }

    public bool IsAuthenticated => Token is not null && Usuario is not null;

    public bool TienePermiso(string permiso) => Usuario?.Permisos.Contains(permiso) ?? false;

    public void Establecer(string token, UsuarioResponse usuario)
    {
        Token = token;
        Usuario = usuario;
    }

    public void Limpiar()
    {
        Token = null;
        Usuario = null;
    }
}
