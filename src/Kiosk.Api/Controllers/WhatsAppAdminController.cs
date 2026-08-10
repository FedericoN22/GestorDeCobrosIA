using Kiosk.Application.CasosUso.Whatsapp;
using Kiosk.Domain.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kiosk.Api.Controllers;

[ApiController]
[Route("api/whatsapp")]
public sealed class WhatsAppAdminController : ApiControllerBase
{
    private readonly ServicioWhatsAppAdmin _servicio;

    public WhatsAppAdminController(ServicioWhatsAppAdmin servicio)
    {
        _servicio = servicio;
    }

    [HttpGet("whitelist")]
    [Authorize(Policy = Permisos.WhatsappOperar)]
    public async Task<ActionResult<IReadOnlyList<WhitelistResponse>>> ListarWhitelist(CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId)
        {
            return Unauthorized();
        }

        var entradas = await _servicio.ListarWhitelistAsync(comercioId, cancellationToken);
        return Ok(entradas.Select(e => new WhitelistResponse(e.Id, e.WhatsappNumero, e.Activo)).ToList());
    }

    [HttpPost("whitelist")]
    [Authorize(Policy = Permisos.WhatsappOperar)]
    public async Task<ActionResult<WhitelistResponse>> AgregarWhitelist(AgregarWhitelistRequest request, CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId || Username is null)
        {
            return Unauthorized();
        }

        var resultado = await _servicio.AgregarWhitelistAsync(
            new AgregarWhitelistCommand(comercioId, request.WhatsappNumero, Username, Canal),
            cancellationToken);

        return resultado.IsSuccess
            ? Ok(new WhitelistResponse(resultado.Value!.Id, resultado.Value.WhatsappNumero, resultado.Value.Activo))
            : ErrorResponse(resultado.Error!);
    }

    [HttpDelete("whitelist/{id:guid}")]
    [Authorize(Policy = Permisos.WhatsappOperar)]
    public async Task<ActionResult<WhitelistResponse>> QuitarWhitelist(Guid id, CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId || Username is null)
        {
            return Unauthorized();
        }

        var resultado = await _servicio.QuitarWhitelistAsync(
            new QuitarWhitelistCommand(comercioId, id, Username, Canal),
            cancellationToken);

        return resultado.IsSuccess
            ? Ok(new WhitelistResponse(resultado.Value!.Id, resultado.Value.WhatsappNumero, resultado.Value.Activo))
            : ErrorResponse(resultado.Error!);
    }

    [HttpGet("config/bot")]
    [Authorize(Policy = Permisos.WhatsappOperar)]
    public async Task<ActionResult<ConfiguracionBotResponse>> ObtenerConfiguracionBot(CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId)
        {
            return Unauthorized();
        }

        var config = await _servicio.ObtenerConfiguracionBotAsync(comercioId, cancellationToken);
        return Ok(new ConfiguracionBotResponse(config.Nombre, config.Bienvenida, config.TiempoConfirmacionMinutos, config.LimiteMensajesPorMinuto));
    }

    [HttpPut("config/bot")]
    [Authorize(Policy = Permisos.WhatsappOperar)]
    public async Task<ActionResult<ConfiguracionBotResponse>> GuardarConfiguracionBot(GuardarConfiguracionBotRequest request, CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId || Username is null)
        {
            return Unauthorized();
        }

        var resultado = await _servicio.GuardarConfiguracionBotAsync(
            new GuardarConfiguracionBotCommand(
                comercioId,
                request.Nombre,
                request.Bienvenida,
                request.TiempoConfirmacionMinutos,
                request.LimiteMensajesPorMinuto,
                Username,
                Canal),
            cancellationToken);

        return resultado.IsSuccess
            ? Ok(new ConfiguracionBotResponse(resultado.Value!.Nombre, resultado.Value.Bienvenida, resultado.Value.TiempoConfirmacionMinutos, resultado.Value.LimiteMensajesPorMinuto))
            : ErrorResponse(resultado.Error!);
    }
}

public sealed record AgregarWhitelistRequest(string WhatsappNumero);

public sealed record WhitelistResponse(Guid Id, string WhatsappNumero, bool Activo);

public sealed record ConfiguracionBotResponse(string Nombre, string Bienvenida, int TiempoConfirmacionMinutos, int LimiteMensajesPorMinuto);

public sealed record GuardarConfiguracionBotRequest(string Nombre, string Bienvenida, int TiempoConfirmacionMinutos, int LimiteMensajesPorMinuto);
