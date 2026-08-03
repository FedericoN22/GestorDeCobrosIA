using System.Text.Json;
using Kiosk.Application.CasosUso.Sync;
using Kiosk.Domain.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kiosk.Api.Controllers;

[Route("api/sync")]
public sealed class SyncController : ApiControllerBase
{
    private readonly ServicioSync _servicio;

    public SyncController(ServicioSync servicio)
    {
        _servicio = servicio;
    }

    [HttpPost("batch")]
    [Authorize(Policy = Permisos.SyncOperar)]
    public async Task<ActionResult<ProcesarBatchResult>> Batch(ProcesarBatchRequest request, CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId || Username is null)
        {
            return Unauthorized();
        }

        if (request.Operaciones is null || request.Operaciones.Count == 0)
        {
            return BadRequest(new { error = "OPERACIONES_REQUERIDAS", message = "El batch debe incluir al menos una operación." });
        }

        var operaciones = request.Operaciones
            .Select(o => new OperacionSyncCommand(o.OperationId, o.Tipo, o.Payload))
            .ToList();

        var resultado = await _servicio.ProcesarBatchAsync(
            new ProcesarBatchCommand(comercioId, UsuarioId, Username, operaciones),
            cancellationToken);

        return Ok(resultado);
    }

    [HttpGet("state")]
    [Authorize(Policy = Permisos.SyncOperar)]
    public async Task<ActionResult<EstadoSyncResult>> Estado(DateTime? cursor, CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId)
        {
            return Unauthorized();
        }

        var estado = await _servicio.ObtenerEstadoAsync(comercioId, cursor, cancellationToken);
        return Ok(estado);
    }

    [HttpPost("ack")]
    [Authorize(Policy = Permisos.SyncOperar)]
    public async Task<ActionResult<ConfirmarSyncResult>> Confirmar(ConfirmarSyncRequest request, CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId)
        {
            return Unauthorized();
        }

        if (request.OperationIds is null)
        {
            return BadRequest(new { error = "OPERATION_IDS_REQUERIDOS", message = "Debe indicar los operationIds a confirmar." });
        }

        var resultado = await _servicio.ConfirmarAsync(comercioId, request.OperationIds, cancellationToken);
        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : ErrorResponse(resultado.Error!);
    }
}

public sealed record OperacionBatchRequest(Guid OperationId, string Tipo, JsonElement? Payload);

public sealed record ProcesarBatchRequest(IReadOnlyList<OperacionBatchRequest>? Operaciones);

public sealed record ConfirmarSyncRequest(IReadOnlyList<Guid>? OperationIds);
