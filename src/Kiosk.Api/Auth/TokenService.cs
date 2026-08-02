using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Kiosk.Domain.Usuarios;
using Microsoft.IdentityModel.Tokens;

namespace Kiosk.Api.Auth;

public sealed class TokenService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiresInMinutes;

    public TokenService(string secretKey, string issuer, string audience, int expiresInMinutes)
    {
        _secretKey = secretKey;
        _issuer = issuer;
        _audience = audience;
        _expiresInMinutes = expiresInMinutes;
    }

    public int ExpiraEnMinutos => _expiresInMinutes;

    public string Generar(Guid usuarioId, Guid comercioId, string username, Rol rol)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuarioId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new("comercio_id", comercioId.ToString()),
            new(ClaimTypes.Role, rol.ToString())
        };

        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expiresInMinutes),
            signingCredentials: credenciales);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
