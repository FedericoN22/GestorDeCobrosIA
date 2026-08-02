using Kiosk.Application.CasosUso.Catalogos;
using Kiosk.Domain.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kiosk.Api.Controllers;

[Route("api/productos")]
public sealed class ProductosController : ApiControllerBase
{
    private readonly ServicioProductos _servicio;

    public ProductosController(ServicioProductos servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    [Authorize(Policy = Permisos.ProductosConsultar)]
    public async Task<ActionResult<IReadOnlyList<ProductoResponse>>> Listar(CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId)
        {
            return Unauthorized();
        }

        var productos = await _servicio.ListarAsync(comercioId, cancellationToken);
        return Ok(productos.Select(ToResponse).ToList());
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permisos.ProductosConsultar)]
    public async Task<ActionResult<ProductoResponse>> Obtener(Guid id, CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId)
        {
            return Unauthorized();
        }

        var resultado = await _servicio.ObtenerAsync(comercioId, id, cancellationToken);
        return resultado.IsSuccess
            ? Ok(ToResponse(resultado.Value!))
            : ErrorResponse(resultado.Error!);
    }

    [HttpPost]
    [Authorize(Policy = Permisos.ProductosGestionar)]
    public async Task<ActionResult<CrearProductoResponse>> Crear(CrearProductoRequest request, CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId || Username is null)
        {
            return Unauthorized();
        }

        var resultado = await _servicio.CrearProductoAsync(
            new CrearProductoCommand(comercioId, request.CategoriaId, request.Nombre, Username, Canal),
            cancellationToken);

        return resultado.IsSuccess
            ? CreatedAtAction(nameof(Obtener), new { id = resultado.Value!.ProductoId },
                new CrearProductoResponse(resultado.Value.ProductoId, resultado.Value.PresentacionId))
            : ErrorResponse(resultado.Error!);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permisos.ProductosGestionar)]
    public async Task<ActionResult<ProductoResponse>> Editar(Guid id, EditarProductoRequest request, CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId || Username is null)
        {
            return Unauthorized();
        }

        var resultado = await _servicio.EditarProductoAsync(
            new EditarProductoCommand(comercioId, id, request.Nombre, request.CategoriaId, Username, Canal),
            cancellationToken);

        return resultado.IsSuccess
            ? Ok(ToResponse(resultado.Value!))
            : ErrorResponse(resultado.Error!);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permisos.ProductosGestionar)]
    public async Task<ActionResult<ProductoResponse>> Desactivar(Guid id, CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId || Username is null)
        {
            return Unauthorized();
        }

        var resultado = await _servicio.DesactivarProductoAsync(
            new DesactivarProductoCommand(comercioId, id, Username, Canal),
            cancellationToken);

        return resultado.IsSuccess
            ? Ok(ToResponse(resultado.Value!))
            : ErrorResponse(resultado.Error!);
    }

    [HttpPost("{id:guid}/presentaciones")]
    [Authorize(Policy = Permisos.ProductosGestionar)]
    public async Task<ActionResult<PresentacionResponse>> AgregarPresentacion(
        Guid id,
        AgregarPresentacionRequest request,
        CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId || Username is null)
        {
            return Unauthorized();
        }

        var resultado = await _servicio.AgregarPresentacionAsync(
            new AgregarPresentacionCommand(
                comercioId,
                id,
                request.Nombre,
                request.PrecioVentaCentavos,
                request.PrecioCostoCentavos,
                request.CodigoBarras,
                Username,
                Canal),
            cancellationToken);

        return resultado.IsSuccess
            ? Ok(new PresentacionResponse(resultado.Value, request.Nombre, request.CodigoBarras,
                request.PrecioVentaCentavos, request.PrecioCostoCentavos, true, 0, null, false))
            : ErrorResponse(resultado.Error!);
    }

    [HttpPut("presentaciones/{presentacionId:guid}")]
    [Authorize(Policy = Permisos.ProductosGestionar)]
    public async Task<ActionResult<PresentacionResponse>> EditarPresentacion(
        Guid presentacionId,
        EditarPresentacionRequest request,
        CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId || Username is null)
        {
            return Unauthorized();
        }

        var resultado = await _servicio.EditarPresentacionAsync(
            new EditarPresentacionCommand(
                comercioId,
                presentacionId,
                request.Nombre,
                request.PrecioVentaCentavos,
                request.PrecioCostoCentavos,
                request.CodigoBarras,
                Username,
                Canal),
            cancellationToken);

        return resultado.IsSuccess
            ? Ok(ToResponse(resultado.Value!))
            : ErrorResponse(resultado.Error!);
    }

    [HttpDelete("presentaciones/{presentacionId:guid}")]
    [Authorize(Policy = Permisos.ProductosGestionar)]
    public async Task<ActionResult<PresentacionResponse>> DesactivarPresentacion(Guid presentacionId, CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId || Username is null)
        {
            return Unauthorized();
        }

        var resultado = await _servicio.DesactivarPresentacionAsync(
            new DesactivarPresentacionCommand(comercioId, presentacionId, Username, Canal),
            cancellationToken);

        return resultado.IsSuccess
            ? Ok(ToResponse(resultado.Value!))
            : ErrorResponse(resultado.Error!);
    }

    private static ProductoResponse ToResponse(ProductoResult producto) => new(
        producto.Id,
        producto.Nombre,
        producto.CategoriaId,
        producto.Activo,
        producto.Presentaciones.Select(ToResponse).ToList());

    private static PresentacionResponse ToResponse(PresentacionResult presentacion) => new(
        presentacion.Id,
        presentacion.Nombre,
        presentacion.CodigoBarras,
        presentacion.PrecioVentaCentavos,
        presentacion.PrecioCostoCentavos,
        presentacion.Activa,
        presentacion.StockActual,
        presentacion.StockMinimo,
        presentacion.StockBajo);
}

public sealed record CrearProductoRequest(string Nombre, Guid? CategoriaId);

public sealed record EditarProductoRequest(string Nombre, Guid? CategoriaId);

public sealed record AgregarPresentacionRequest(string Nombre, int PrecioVentaCentavos, int? PrecioCostoCentavos, string? CodigoBarras);

public sealed record EditarPresentacionRequest(string Nombre, int PrecioVentaCentavos, int? PrecioCostoCentavos, string? CodigoBarras);

public sealed record CrearProductoResponse(Guid ProductoId, Guid? PresentacionId);

public sealed record ProductoResponse(Guid Id, string Nombre, Guid? CategoriaId, bool Activo, IReadOnlyList<PresentacionResponse> Presentaciones);

public sealed record PresentacionResponse(Guid Id, string Nombre, string? CodigoBarras, int PrecioVentaCentavos, int? PrecioCostoCentavos, bool Activa, int StockActual, int? StockMinimo, bool StockBajo);
