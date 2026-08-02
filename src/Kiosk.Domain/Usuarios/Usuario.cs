using Kiosk.Domain.Common;

namespace Kiosk.Domain.Usuarios;

public enum Rol
{
    ADMIN = 1,
    CAJERO = 2
}

public class Usuario
{
    public Guid Id { get; private set; }
    public Guid ComercioId { get; private set; }
    public string Nombre { get; private set; } = null!;
    public string Username { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public Rol Rol { get; private set; }
    public bool Activo { get; private set; }

    private Usuario() { }

    public static Usuario Crear(Guid comercioId, string nombre, string username, string passwordHash, Rol rol)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new DomainException("USUARIO_NOMBRE_REQUERIDO", "El nombre del usuario es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new DomainException("USUARIO_USERNAME_REQUERIDO", "El nombre de usuario es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("USUARIO_PASSWORD_REQUERIDA", "La contraseña es obligatoria.");
        }

        return new Usuario
        {
            Id = Guid.NewGuid(),
            ComercioId = comercioId,
            Nombre = nombre.Trim(),
            Username = username.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            Rol = rol,
            Activo = true
        };
    }

    public void CambiarPassword(string nuevoPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(nuevoPasswordHash))
        {
            throw new DomainException("USUARIO_PASSWORD_REQUERIDA", "La contraseña es obligatoria.");
        }

        PasswordHash = nuevoPasswordHash;
    }

    public void Desactivar()
    {
        Activo = false;
    }
}
