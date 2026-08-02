using Kiosk.Domain.Usuarios;

namespace Kiosk.Domain.Tests;

public class UsuarioTests
{
    [Fact]
    public void Crear_ConDatosValidos_AsignaPropiedades()
    {
        var usuario = Usuario.Crear(Guid.NewGuid(), "  Juan Pérez  ", "  Admin ", "hash-bcrypt", Rol.ADMIN);

        Assert.Equal("Juan Pérez", usuario.Nombre);
        Assert.Equal("admin", usuario.Username);
        Assert.Equal("hash-bcrypt", usuario.PasswordHash);
        Assert.Equal(Rol.ADMIN, usuario.Rol);
        Assert.True(usuario.Activo);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_ConNombreVacio_LanzaError(string? nombre)
    {
        AssertHelper.ThrowsDomain(
            "USUARIO_NOMBRE_REQUERIDO",
            () => Usuario.Crear(Guid.NewGuid(), nombre!, "admin", "hash", Rol.ADMIN));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_ConUsernameVacio_LanzaError(string? username)
    {
        AssertHelper.ThrowsDomain(
            "USUARIO_USERNAME_REQUERIDO",
            () => Usuario.Crear(Guid.NewGuid(), "Juan", username!, "hash", Rol.ADMIN));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_ConPasswordVacia_LanzaError(string? passwordHash)
    {
        AssertHelper.ThrowsDomain(
            "USUARIO_PASSWORD_REQUERIDA",
            () => Usuario.Crear(Guid.NewGuid(), "Juan", "admin", passwordHash!, Rol.ADMIN));
    }

    [Fact]
    public void CambiarPassword_ActualizaHash()
    {
        var usuario = Usuario.Crear(Guid.NewGuid(), "Juan", "admin", "hash-viejo", Rol.CAJERO);
        usuario.CambiarPassword("hash-nuevo");

        Assert.Equal("hash-nuevo", usuario.PasswordHash);
    }

    [Fact]
    public void CambiarPassword_Vacia_LanzaError()
    {
        var usuario = Usuario.Crear(Guid.NewGuid(), "Juan", "admin", "hash", Rol.CAJERO);

        AssertHelper.ThrowsDomain("USUARIO_PASSWORD_REQUERIDA", () => usuario.CambiarPassword("  "));
    }

    [Fact]
    public void Desactivar_PoneActivoEnFalse()
    {
        var usuario = Usuario.Crear(Guid.NewGuid(), "Juan", "admin", "hash", Rol.CAJERO);
        usuario.Desactivar();

        Assert.False(usuario.Activo);
    }
}
