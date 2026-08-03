using Kiosk.Application.CasosUso.Ventas;
using Kiosk.Domain.Usuarios;
using Kiosk.Domain.Ventas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kiosk.Api.Controllers;

[Route("api/cajas")]
public sealed class CajasController : ApiControllerBase
{
    private readonly ServicioCaja _servicio;

    public CajasController(ServicioCaja servicio)
    {
        _servicio = servicio;
    }

    [HttpPost("abrir")]
    [Authorize(Policy = Permisos.CajasAbrir)]
    public async Task<ActionResult<AbrirCajaResponse>> Abrir(AbrirCajaRequest request, CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId || UsuarioId is not Guid usuarioId || Username is null)
        {
            return Unauthorized();
        }

        var resultado = await _servicio.AbrirAsync(
            new AbrirCajaCommand(comercioId, usuarioId, request.MontoInicialCentavos, Username, Canal),
            cancellationToken);

        return resultado.IsSuccess
            ? Ok(new AbrirCajaResponse(resultado.Value!))
            : ErrorResponse(resultado.Error!);
    }

    [HttpPost("cerrar")]
    [Authorize(Policy = Permisos.CajasCerrar)]
    public async Task<ActionResult<CerrarCajaResponse>> Cerrar(CerrarCajaRequest request, CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId || Username is null)
        {
            return Unauthorized();
        }

        var resultado = await _servicio.CerrarAsync(
            new CerrarCajaCommand(comercioId, request.MontoEsperadoCentavos, request.MontoDeclaradoCentavos, Username, Canal),
            cancellationToken);

        return resultado.IsSuccess
            ? Ok(new CerrarCajaResponse(resultado.Value!.CajaId, resultado.Value.DiferenciaCentavos))
            : ErrorResponse(resultado.Error!);
    }

    [HttpGet("activa")]
    [Authorize(Policy = Permisos.CajasConsultar)]
    public async Task<ActionResult<CajaResponse?>> Activa(CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId)
        {
            return Unauthorized();
        }

        var caja = await _servicio.ObtenerActivaAsync(comercioId, cancellationToken);
        return caja is null ? Ok(null) : Ok(ToResponse(caja));
    }

    private static CajaResponse ToResponse(Caja caja) => new(
        caja.Id,
        caja.FechaApertura,
        caja.MontoInicialCentavos,
        caja.Estado,
        caja.FechaCierre,
        caja.MontoEsperadoCentavos,
        caja.MontoDeclaradoCentavos,
        caja.DiferenciaCentavos);
}

public sealed record AbrirCajaRequest(int MontoInicialCentavos);

public sealed record CerrarCajaRequest(int MontoEsperadoCentavos, int MontoDeclaradoCentavos);

public sealed record AbrirCajaResponse(Guid CajaId);

public sealed record CerrarCajaResponse(Guid CajaId, int DiferenciaCentavos);

public sealed record CajaResponse(
    Guid Id,
    DateTime FechaApertura,
    int MontoInicialCentavos,
    EstadoCaja Estado,
    DateTime? FechaCierre,
    int? MontoEsperadoCentavos,
    int? MontoDeclaradoCentavos,
    int? DiferenciaCentavos);
