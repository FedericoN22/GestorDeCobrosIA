using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Kiosk.Application.Abstractions;
using Kiosk.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Kiosk.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected Guid? ComercioId =>
        Guid.TryParse(User.FindFirst("comercio_id")?.Value, out var id) ? id : null;

    protected Guid? UsuarioId =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

    protected string? Username =>
        User.FindFirst(ClaimTypes.Name)?.Value
        ?? User.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value;

    protected Canal Canal => Canal.WEB;

    protected ActionResult ErrorResponse(Error error) =>
        BadRequest(new { error = error.Code, message = error.Message });
}
