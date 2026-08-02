using Kiosk.Application.Abstractions;
using Kiosk.Application.Puertos;
using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Usuarios;

namespace Kiosk.Application.CasosUso.Autenticacion;

public sealed record LoginCommand(string Username, string Password);

public sealed record LoginResult(Guid UsuarioId, string Username, string Nombre, Rol Rol, Guid ComercioId);

public sealed class ServicioAutenticacion
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IPasswordHasher _passwordHasher;

    public ServicioAutenticacion(IUsuarioRepository usuarios, IPasswordHasher passwordHasher)
    {
        _usuarios = usuarios;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<LoginResult>> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        var username = command.Username.Trim().ToLowerInvariant();
        var usuario = await _usuarios.GetByUsernameAsync(username, cancellationToken);

        if (usuario is null || !usuario.Activo || !_passwordHasher.Verify(command.Password, usuario.PasswordHash))
        {
            return Result<LoginResult>.Fail(
                new Error("AUTH_CREDENCIALES_INVALIDAS", "Usuario o contraseña incorrectos."));
        }

        return Result<LoginResult>.Ok(new LoginResult(usuario.Id, usuario.Username, usuario.Nombre, usuario.Rol, usuario.ComercioId));
    }
}
