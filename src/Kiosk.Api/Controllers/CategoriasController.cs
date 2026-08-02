using Kiosk.Application.CasosUso.Catalogos;
using Kiosk.Domain.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kiosk.Api.Controllers;

[Route("api/categorias")]
public sealed class CategoriasController : ApiControllerBase
{
    private readonly ServicioCategorias _servicio;

    public CategoriasController(ServicioCategorias servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    [Authorize(Policy = Permisos.ProductosConsultar)]
    public async Task<ActionResult<IReadOnlyList<CategoriaResponse>>> Listar(CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId)
        {
            return Unauthorized();
        }

        var categorias = await _servicio.ListarAsync(comercioId, cancellationToken);
        return Ok(categorias.Select(ToResponse).ToList());
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permisos.ProductosConsultar)]
    public async Task<ActionResult<CategoriaResponse>> Obtener(Guid id, CancellationToken cancellationToken)
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
    public async Task<ActionResult<CategoriaResponse>> Crear(CrearCategoriaRequest request, CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId || Username is null)
        {
            return Unauthorized();
        }

        var resultado = await _servicio.CrearAsync(
            new CrearCategoriaCommand(comercioId, request.Nombre, Username, Canal),
            cancellationToken);

        return resultado.IsSuccess
            ? CreatedAtAction(nameof(Obtener), new { id = resultado.Value!.Id }, ToResponse(resultado.Value))
            : ErrorResponse(resultado.Error!);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permisos.ProductosGestionar)]
    public async Task<ActionResult<CategoriaResponse>> Editar(Guid id, EditarCategoriaRequest request, CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId || Username is null)
        {
            return Unauthorized();
        }

        var resultado = await _servicio.EditarAsync(
            new EditarCategoriaCommand(comercioId, id, request.Nombre, Username, Canal),
            cancellationToken);

        return resultado.IsSuccess
            ? Ok(ToResponse(resultado.Value!))
            : ErrorResponse(resultado.Error!);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permisos.ProductosGestionar)]
    public async Task<ActionResult<CategoriaResponse>> Desactivar(Guid id, CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId || Username is null)
        {
            return Unauthorized();
        }

        var resultado = await _servicio.DesactivarAsync(
            new DesactivarCategoriaCommand(comercioId, id, Username, Canal),
            cancellationToken);

        return resultado.IsSuccess
            ? Ok(ToResponse(resultado.Value!))
            : ErrorResponse(resultado.Error!);
    }

    private static CategoriaResponse ToResponse(CategoriaResult categoria) => new(categoria.Id, categoria.Nombre, categoria.Activa);
}

public sealed record CrearCategoriaRequest(string Nombre);

public sealed record EditarCategoriaRequest(string Nombre);

public sealed record CategoriaResponse(Guid Id, string Nombre, bool Activa);
