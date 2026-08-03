using Kiosk.Application.CasosUso.Ventas;
using Kiosk.Domain.Usuarios;
using Kiosk.Domain.Ventas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kiosk.Api.Controllers;

[Route("api/ventas")]
public sealed class VentasController : ApiControllerBase
{
    private readonly ServicioVentas _servicio;

    public VentasController(ServicioVentas servicio)
    {
        _servicio = servicio;
    }

    [HttpPost]
    [Authorize(Policy = Permisos.VentasRegistrar)]
    public async Task<ActionResult<RegistrarVentaResponse>> Registrar(RegistrarVentaRequest request, CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId || Username is null)
        {
            return Unauthorized();
        }

        var resultado = await _servicio.RegistrarAsync(
            new RegistrarVentaCommand(
                comercioId,
                UsuarioId,
                Username,
                Canal,
                request.Lineas
                    .Select(l => new LineaVentaCommand(l.PresentacionId, l.Cantidad))
                    .ToList(),
                request.Pagos
                    .Select(p => new PagoCommand(p.Medio, p.MontoCentavos))
                    .ToList()),
            cancellationToken);

        return resultado.IsSuccess
            ? Ok(new RegistrarVentaResponse(
                resultado.Value!.VentaId,
                resultado.Value.Numero,
                resultado.Value.TotalCentavos,
                resultado.Value.VueltoCentavos))
            : ErrorResponse(resultado.Error!);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permisos.VentasConsultar)]
    public async Task<ActionResult<VentaResponse>> Obtener(Guid id, CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId)
        {
            return Unauthorized();
        }

        var venta = await _servicio.ObtenerAsync(comercioId, id, cancellationToken);
        if (venta is null)
        {
            return NotFound(new { error = "VENTA_NO_ENCONTRADA", message = "La venta no existe o no pertenece a este comercio." });
        }

        return Ok(ToResponse(venta));
    }

    private static VentaResponse ToResponse(Venta venta) => new(
        venta.Id,
        venta.ComercioId,
        venta.CajaId,
        venta.Numero,
        venta.TotalCentavos,
        venta.TotalPagadoCentavos,
        venta.Fecha,
        venta.ClientGenerated,
        venta.Lineas.Select(l => new LineaVentaResponse(
            l.Id,
            l.PresentacionId,
            l.ProductoNombre,
            l.PresentacionNombre,
            l.Cantidad,
            l.PrecioUnitarioCentavos,
            l.SubtotalCentavos)).ToList(),
        venta.Pagos.Select(p => new PagoResponse(p.Id, p.Medio, p.MontoCentavos)).ToList());
}

public sealed record RegistrarVentaRequest(
    IReadOnlyList<LineaVentaRequest> Lineas,
    IReadOnlyList<PagoRequest> Pagos);

public sealed record LineaVentaRequest(Guid PresentacionId, int Cantidad);

public sealed record PagoRequest(MedioPago Medio, int MontoCentavos);

public sealed record RegistrarVentaResponse(Guid VentaId, int Numero, int TotalCentavos, int VueltoCentavos);

public sealed record VentaResponse(
    Guid Id,
    Guid ComercioId,
    Guid CajaId,
    int Numero,
    int TotalCentavos,
    int TotalPagadoCentavos,
    DateTime Fecha,
    bool ClientGenerated,
    IReadOnlyList<LineaVentaResponse> Lineas,
    IReadOnlyList<PagoResponse> Pagos);

public sealed record LineaVentaResponse(
    Guid Id,
    Guid PresentacionId,
    string ProductoNombre,
    string PresentacionNombre,
    int Cantidad,
    int PrecioUnitarioCentavos,
    int SubtotalCentavos);

public sealed record PagoResponse(Guid Id, MedioPago Medio, int MontoCentavos);
