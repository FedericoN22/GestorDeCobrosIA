using Kiosk.Application.CasosUso.Chat;
using Kiosk.Domain.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kiosk.Api.Controllers;

[ApiController]
[Route("api/chat")]
public sealed class ChatController : ApiControllerBase
{
    private readonly ServicioChat _servicio;

    public ChatController(ServicioChat servicio)
    {
        _servicio = servicio;
    }

    [HttpPost]
    [Authorize(Policy = Permisos.StockGestionar)]
    public async Task<ActionResult<ChatResponse>> Enviar(ChatRequest request, CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId || UsuarioId is not Guid usuarioId)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Texto))
        {
            return BadRequest(new { error = "CHAT_TEXTO_REQUERIDO", message = "El campo 'texto' es obligatorio." });
        }

        var respuesta = await _servicio.ProcesarAsync(comercioId, usuarioId, request.Texto, cancellationToken);
        return Ok(new ChatResponse(respuesta));
    }
}

public sealed record ChatRequest(string Texto);

public sealed record ChatResponse(string Respuesta);
