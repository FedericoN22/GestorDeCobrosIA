using Kiosk.Application.CasosUso.Stock;
using Kiosk.Domain.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kiosk.Api.Controllers;

[Route("api/stock")]
public sealed class StockController : ApiControllerBase
{
    private readonly ServicioStock _servicio;

    public StockController(ServicioStock servicio)
    {
        _servicio = servicio;
    }

    [HttpPost("entrada")]
    [Authorize(Policy = Permisos.StockGestionar)]
    public async Task<ActionResult<StockActualResponse>> Entrada(EntradaRequest request, CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId || Username is null)
        {
            return Unauthorized();
        }

        var resultado = await _servicio.EntradaManualAsync(
            new EntradaManualCommand(
                comercioId,
                request.PresentacionId,
                request.Cantidad,
                UsuarioId,
                Username,
                Canal,
                request.PrecioCostoCentavos),
            cancellationToken);

        return resultado.IsSuccess
            ? Ok(ToResponse(resultado.Value!))
            : ErrorResponse(resultado.Error!);
    }

    [HttpPost("ajuste")]
    [Authorize(Policy = Permisos.StockGestionar)]
    public async Task<ActionResult<StockActualResponse>> Ajuste(AjusteRequest request, CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId || Username is null)
        {
            return Unauthorized();
        }

        var resultado = await _servicio.AjusteAsync(
            new AjusteStockCommand(
                comercioId,
                request.PresentacionId,
                request.Cantidad,
                request.Motivo,
                UsuarioId,
                Username,
                Canal),
            cancellationToken);

        return resultado.IsSuccess
            ? Ok(ToResponse(resultado.Value!))
            : ErrorResponse(resultado.Error!);
    }

    [HttpPut("presentaciones/{presentacionId:guid}/stock-minimo")]
    [Authorize(Policy = Permisos.StockGestionar)]
    public async Task<ActionResult<StockActualResponse>> ConfigurarStockMinimo(
        Guid presentacionId,
        ConfigurarStockMinimoRequest request,
        CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId || Username is null)
        {
            return Unauthorized();
        }

        var resultado = await _servicio.ConfigurarStockMinimoAsync(
            new ConfigurarStockMinimoCommand(comercioId, presentacionId, request.StockMinimo, Username, Canal),
            cancellationToken);

        return resultado.IsSuccess
            ? Ok(ToResponse(resultado.Value!))
            : ErrorResponse(resultado.Error!);
    }

    private static StockActualResponse ToResponse(StockActualResult stock) => new(
        stock.PresentacionId,
        stock.StockActual,
        stock.StockMinimo,
        stock.StockBajo);
}

public sealed record EntradaRequest(Guid PresentacionId, int Cantidad, int? PrecioCostoCentavos);

public sealed record AjusteRequest(Guid PresentacionId, int Cantidad, string Motivo);

public sealed record ConfigurarStockMinimoRequest(int? StockMinimo);

public sealed record StockActualResponse(Guid PresentacionId, int StockActual, int? StockMinimo, bool StockBajo);
