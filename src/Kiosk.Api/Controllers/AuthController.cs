using Kiosk.Api.Auth;
using Kiosk.Application.Abstractions;
using Kiosk.Application.CasosUso.Autenticacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kiosk.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ServicioAutenticacion _autenticacion;
    private readonly TokenService _tokenService;

    public AuthController(ServicioAutenticacion autenticacion, TokenService tokenService)
    {
        _autenticacion = autenticacion;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var resultado = await _autenticacion.LoginAsync(new LoginCommand(request.Username, request.Password), cancellationToken);
        if (!resultado.IsSuccess)
        {
            return BadRequest(new { error = resultado.Error!.Code, message = resultado.Error.Message });
        }

        var usuario = resultado.Value!;
        var token = _tokenService.Generar(usuario.UsuarioId, usuario.ComercioId, usuario.Username, usuario.Rol);

        return Ok(new LoginResponse(
            token,
            DateTime.UtcNow.AddMinutes(_tokenService.ExpiraEnMinutos),
            new UsuarioResponse(usuario.UsuarioId, usuario.Username, usuario.Nombre, usuario.Rol.ToString(), usuario.ComercioId)));
    }
}

public sealed record LoginRequest(string Username, string Password);

public sealed record UsuarioResponse(Guid Id, string Username, string Nombre, string Rol, Guid ComercioId);

public sealed record LoginResponse(string Token, DateTime ExpiraEn, UsuarioResponse Usuario);
