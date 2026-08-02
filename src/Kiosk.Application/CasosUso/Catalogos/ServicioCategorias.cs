using Kiosk.Application.Abstractions;
using Kiosk.Application.Auditoria;
using Kiosk.Application.Puertos;
using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Auditoria;
using Kiosk.Domain.Catalogos;
using Kiosk.Domain.Common;

namespace Kiosk.Application.CasosUso.Catalogos;

public sealed record CrearCategoriaCommand(Guid ComercioId, string Nombre, string Actor, Canal Origen);

public sealed record EditarCategoriaCommand(Guid ComercioId, Guid CategoriaId, string Nombre, string Actor, Canal Origen);

public sealed record DesactivarCategoriaCommand(Guid ComercioId, Guid CategoriaId, string Actor, Canal Origen);

public sealed record CategoriaResult(Guid Id, string Nombre, bool Activa);

public sealed class ServicioCategorias
{
    private readonly ICategoriaRepository _categorias;
    private readonly IAuditoriaRepository _auditoria;
    private readonly IUnitOfWork _unitOfWork;

    public ServicioCategorias(ICategoriaRepository categorias, IAuditoriaRepository auditoria, IUnitOfWork unitOfWork)
    {
        _categorias = categorias;
        _auditoria = auditoria;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CategoriaResult>> CrearAsync(CrearCategoriaCommand command, CancellationToken cancellationToken = default)
    {
        if (await _categorias.ExisteNombreAsync(command.ComercioId, command.Nombre, cancellationToken: cancellationToken))
        {
            return Result<CategoriaResult>.Fail(
                new Error("CATEGORIA_DUPLICADA", $"Ya existe una categoría llamada '{command.Nombre}'."));
        }

        var categoria = Categoria.Crear(command.ComercioId, command.Nombre);
        _categorias.Add(categoria);
        AuditoriaRegistrador.Registrar(_auditoria, command.ComercioId, command.Origen, command.Actor, AuditoriaTipos.CategoriaCreada, new { categoria.Id, categoria.Nombre });
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CategoriaResult>.Ok(ToResult(categoria));
    }

    public async Task<Result<CategoriaResult>> EditarAsync(EditarCategoriaCommand command, CancellationToken cancellationToken = default)
    {
        var categoria = await _categorias.GetByIdAsync(command.CategoriaId, cancellationToken);
        if (categoria is null || categoria.ComercioId != command.ComercioId)
        {
            return Result<CategoriaResult>.Fail(new Error("CATEGORIA_NO_ENCONTRADA", "La categoría no existe o no pertenece al comercio."));
        }

        if (await _categorias.ExisteNombreAsync(command.ComercioId, command.Nombre, categoria.Id, cancellationToken))
        {
            return Result<CategoriaResult>.Fail(
                new Error("CATEGORIA_DUPLICADA", $"Ya existe una categoría llamada '{command.Nombre}'."));
        }

        categoria.CambiarNombre(command.Nombre);
        AuditoriaRegistrador.Registrar(_auditoria, command.ComercioId, command.Origen, command.Actor, AuditoriaTipos.CategoriaEditada, new { categoria.Id, categoria.Nombre });
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CategoriaResult>.Ok(ToResult(categoria));
    }

    public async Task<Result<CategoriaResult>> DesactivarAsync(DesactivarCategoriaCommand command, CancellationToken cancellationToken = default)
    {
        var categoria = await _categorias.GetByIdAsync(command.CategoriaId, cancellationToken);
        if (categoria is null || categoria.ComercioId != command.ComercioId)
        {
            return Result<CategoriaResult>.Fail(new Error("CATEGORIA_NO_ENCONTRADA", "La categoría no existe o no pertenece al comercio."));
        }

        categoria.Desactivar();
        AuditoriaRegistrador.Registrar(_auditoria, command.ComercioId, command.Origen, command.Actor, AuditoriaTipos.CategoriaDesactivada, new { categoria.Id });
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CategoriaResult>.Ok(ToResult(categoria));
    }

    public async Task<IReadOnlyList<CategoriaResult>> ListarAsync(Guid comercioId, CancellationToken cancellationToken = default)
    {
        var categorias = await _categorias.GetActivasAsync(comercioId, cancellationToken);
        return categorias.Select(ToResult).ToList();
    }

    public async Task<Result<CategoriaResult>> ObtenerAsync(Guid comercioId, Guid id, CancellationToken cancellationToken = default)
    {
        var categoria = await _categorias.GetByIdAsync(id, cancellationToken);
        if (categoria is null || categoria.ComercioId != comercioId)
        {
            return Result<CategoriaResult>.Fail(new Error("CATEGORIA_NO_ENCONTRADA", "La categoría no existe o no pertenece al comercio."));
        }

        return Result<CategoriaResult>.Ok(ToResult(categoria));
    }

    private static CategoriaResult ToResult(Categoria categoria) => new(categoria.Id, categoria.Nombre, categoria.Activa);
}
