using Kiosk.Application.Abstractions;
using Kiosk.Application.Puertos;
using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Application.Auditoria;
using Kiosk.Domain.Auditoria;
using Kiosk.Domain.Catalogos;
using Kiosk.Domain.Common;

namespace Kiosk.Application.CasosUso.Catalogos;

public sealed record CrearProductoCommand(Guid ComercioId, Guid? CategoriaId, string Nombre, string Actor, Canal Origen);

public sealed record AgregarPresentacionCommand(
    Guid ComercioId,
    Guid ProductoId,
    string Nombre,
    int PrecioVentaCentavos,
    int? PrecioCostoCentavos,
    string? CodigoBarras,
    string Actor,
    Canal Origen);

public sealed record EditarProductoCommand(Guid ComercioId, Guid ProductoId, string Nombre, Guid? CategoriaId, string Actor, Canal Origen);

public sealed record DesactivarProductoCommand(Guid ComercioId, Guid ProductoId, string Actor, Canal Origen);

public sealed record EditarPresentacionCommand(
    Guid ComercioId,
    Guid PresentacionId,
    string Nombre,
    int PrecioVentaCentavos,
    int? PrecioCostoCentavos,
    string? CodigoBarras,
    string Actor,
    Canal Origen);

public sealed record DesactivarPresentacionCommand(Guid ComercioId, Guid PresentacionId, string Actor, Canal Origen);

public sealed record CrearProductoResult(Guid ProductoId, Guid? PresentacionId);

public sealed record ProductoResult(
    Guid Id,
    string Nombre,
    Guid? CategoriaId,
    bool Activo,
    IReadOnlyList<PresentacionResult> Presentaciones);

public sealed record PresentacionResult(
    Guid Id,
    string Nombre,
    string? CodigoBarras,
    int PrecioVentaCentavos,
    int? PrecioCostoCentavos,
    bool Activa,
    int StockActual,
    int? StockMinimo,
    bool StockBajo);

public sealed class ServicioProductos
{
    private readonly IProductRepository _productos;
    private readonly ICategoriaRepository _categorias;
    private readonly IAuditoriaRepository _auditoria;
    private readonly IUnitOfWork _unitOfWork;

    public ServicioProductos(IProductRepository productos, ICategoriaRepository categorias, IAuditoriaRepository auditoria, IUnitOfWork unitOfWork)
    {
        _productos = productos;
        _categorias = categorias;
        _auditoria = auditoria;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CrearProductoResult>> CrearProductoAsync(
        CrearProductoCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.CategoriaId.HasValue && !await ExisteCategoriaAsync(command.ComercioId, command.CategoriaId.Value, cancellationToken))
        {
            return Result<CrearProductoResult>.Fail(new Error("CATEGORIA_NO_ENCONTRADA", "La categoría no existe o no pertenece al comercio."));
        }

        var nombreNormalizado = Normalizacion.Normalizar(command.Nombre);
        if (await _productos.ExisteNombreAsync(command.ComercioId, nombreNormalizado, cancellationToken: cancellationToken))
        {
            return Result<CrearProductoResult>.Fail(
                new Error("PRODUCTO_DUPLICADO", $"Ya existe un producto llamado '{command.Nombre}'."));
        }

        var producto = Producto.Crear(command.ComercioId, command.CategoriaId, command.Nombre);
        _productos.Add(producto);
        AuditoriaRegistrador.Registrar(_auditoria, command.ComercioId, command.Origen, command.Actor, AuditoriaTipos.ProductoCreado, new { producto.Id, producto.Nombre });
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CrearProductoResult>.Ok(new CrearProductoResult(producto.Id, null));
    }

    public async Task<Result<Guid>> AgregarPresentacionAsync(
        AgregarPresentacionCommand command,
        CancellationToken cancellationToken = default)
    {
        var producto = await _productos.GetByIdAsync(command.ProductoId, cancellationToken);
        if (producto is null || !producto.Activo || producto.ComercioId != command.ComercioId)
        {
            return Result<Guid>.Fail(new Error("PRODUCTO_NO_ENCONTRADO", "El producto no existe o no pertenece al comercio."));
        }

        if (await ExisteCodigoBarrasAsync(command.ComercioId, command.CodigoBarras, null, cancellationToken))
        {
            return Result<Guid>.Fail(
                new Error("CODIGO_BARRAS_DUPLICADO", $"El código de barras '{command.CodigoBarras}' ya está en uso."));
        }

        var presentacion = producto.AgregarPresentacion(
            command.Nombre,
            command.PrecioVentaCentavos,
            command.PrecioCostoCentavos,
            command.CodigoBarras);

        _productos.AddPresentacion(presentacion);

        AuditoriaRegistrador.Registrar(_auditoria, command.ComercioId, command.Origen, command.Actor,
            AuditoriaTipos.PresentacionCreada,
            new { presentacion.Id, NombreProducto = producto.Nombre, presentacion.Nombre });
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Ok(presentacion.Id);
    }

    public async Task<Result<ProductoResult>> EditarProductoAsync(
        EditarProductoCommand command,
        CancellationToken cancellationToken = default)
    {
        var producto = await GetProductoAsync(command.ComercioId, command.ProductoId, cancellationToken);
        if (producto is null)
        {
            return Result<ProductoResult>.Fail(new Error("PRODUCTO_NO_ENCONTRADO", "El producto no existe o no pertenece al comercio."));
        }

        if (command.CategoriaId.HasValue && !await ExisteCategoriaAsync(command.ComercioId, command.CategoriaId.Value, cancellationToken))
        {
            return Result<ProductoResult>.Fail(new Error("CATEGORIA_NO_ENCONTRADA", "La categoría no existe o no pertenece al comercio."));
        }

        var nombreNormalizado = Normalizacion.Normalizar(command.Nombre);
        if (await _productos.ExisteNombreAsync(command.ComercioId, nombreNormalizado, producto.Id, cancellationToken))
        {
            return Result<ProductoResult>.Fail(
                new Error("PRODUCTO_DUPLICADO", $"Ya existe un producto llamado '{command.Nombre}'."));
        }

        producto.CambiarNombre(command.Nombre);
        producto.CambiarCategoria(command.CategoriaId);
        AuditoriaRegistrador.Registrar(_auditoria, command.ComercioId, command.Origen, command.Actor, AuditoriaTipos.ProductoEditado, new { producto.Id, producto.Nombre, producto.CategoriaId });
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ProductoResult>.Ok(ToResult(producto));
    }

    public async Task<Result<ProductoResult>> DesactivarProductoAsync(
        DesactivarProductoCommand command,
        CancellationToken cancellationToken = default)
    {
        var producto = await GetProductoAsync(command.ComercioId, command.ProductoId, cancellationToken);
        if (producto is null)
        {
            return Result<ProductoResult>.Fail(new Error("PRODUCTO_NO_ENCONTRADO", "El producto no existe o no pertenece al comercio."));
        }

        producto.Desactivar();
        AuditoriaRegistrador.Registrar(_auditoria, command.ComercioId, command.Origen, command.Actor, AuditoriaTipos.ProductoDesactivado, new { producto.Id });
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ProductoResult>.Ok(ToResult(producto));
    }

    public async Task<Result<PresentacionResult>> EditarPresentacionAsync(
        EditarPresentacionCommand command,
        CancellationToken cancellationToken = default)
    {
        var producto = await _productos.GetByPresentacionIdAsync(command.PresentacionId, cancellationToken);
        if (producto is null || producto.ComercioId != command.ComercioId)
        {
            return Result<PresentacionResult>.Fail(new Error("PRESENTACION_NO_ENCONTRADA", "La presentación no existe o no pertenece al comercio."));
        }

        var presentacion = producto.Presentaciones.FirstOrDefault(p => p.Id == command.PresentacionId);
        if (presentacion is null)
        {
            return Result<PresentacionResult>.Fail(new Error("PRESENTACION_NO_ENCONTRADA", "La presentación no existe o no pertenece al comercio."));
        }

        if (producto.Presentaciones.Any(p => p.Id != presentacion.Id
            && string.Equals(p.Nombre, command.Nombre.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return Result<PresentacionResult>.Fail(
                new Error("PRESENTACION_DUPLICADA", $"Ya existe la presentación '{command.Nombre}' para el producto '{producto.Nombre}'."));
        }

        if (await ExisteCodigoBarrasAsync(command.ComercioId, command.CodigoBarras, presentacion.Id, cancellationToken))
        {
            return Result<PresentacionResult>.Fail(
                new Error("CODIGO_BARRAS_DUPLICADO", $"El código de barras '{command.CodigoBarras}' ya está en uso."));
        }

        presentacion.CambiarNombre(command.Nombre);
        presentacion.CambiarPrecioVenta(command.PrecioVentaCentavos);
        presentacion.CambiarPrecioCosto(command.PrecioCostoCentavos);
        presentacion.CambiarCodigoBarras(command.CodigoBarras);

        AuditoriaRegistrador.Registrar(_auditoria, command.ComercioId, command.Origen, command.Actor, AuditoriaTipos.PresentacionEditada,
            new { presentacion.Id, NombreProducto = producto.Nombre, presentacion.Nombre, presentacion.PrecioVentaCentavos, presentacion.CodigoBarras });
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<PresentacionResult>.Ok(ToResult(presentacion));
    }

    public async Task<Result<PresentacionResult>> DesactivarPresentacionAsync(
        DesactivarPresentacionCommand command,
        CancellationToken cancellationToken = default)
    {
        var producto = await _productos.GetByPresentacionIdAsync(command.PresentacionId, cancellationToken);
        if (producto is null || producto.ComercioId != command.ComercioId)
        {
            return Result<PresentacionResult>.Fail(new Error("PRESENTACION_NO_ENCONTRADA", "La presentación no existe o no pertenece al comercio."));
        }

        var presentacion = producto.Presentaciones.FirstOrDefault(p => p.Id == command.PresentacionId);
        if (presentacion is null)
        {
            return Result<PresentacionResult>.Fail(new Error("PRESENTACION_NO_ENCONTRADA", "La presentación no existe o no pertenece al comercio."));
        }

        presentacion.Desactivar();
        AuditoriaRegistrador.Registrar(_auditoria, command.ComercioId, command.Origen, command.Actor, AuditoriaTipos.PresentacionDesactivada,
            new { presentacion.Id, NombreProducto = producto.Nombre, presentacion.Nombre });
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<PresentacionResult>.Ok(ToResult(presentacion));
    }

    public async Task<IReadOnlyList<ProductoResult>> ListarAsync(Guid comercioId, CancellationToken cancellationToken = default)
    {
        var productos = await _productos.GetActivosAsync(comercioId, cancellationToken);
        return productos.Select(ToResult).ToList();
    }

    public async Task<Result<ProductoResult>> ObtenerAsync(Guid comercioId, Guid productoId, CancellationToken cancellationToken = default)
    {
        var producto = await GetProductoAsync(comercioId, productoId, cancellationToken);
        if (producto is null)
        {
            return Result<ProductoResult>.Fail(new Error("PRODUCTO_NO_ENCONTRADO", "El producto no existe o no pertenece al comercio."));
        }

        return Result<ProductoResult>.Ok(ToResult(producto));
    }

    private async Task<Producto?> GetProductoAsync(Guid comercioId, Guid productoId, CancellationToken cancellationToken)
    {
        var producto = await _productos.GetByIdAsync(productoId, cancellationToken);
        return producto is not null && producto.ComercioId == comercioId ? producto : null;
    }

    private async Task<bool> ExisteCategoriaAsync(Guid comercioId, Guid categoriaId, CancellationToken cancellationToken)
    {
        var categoria = await _categorias.GetByIdAsync(categoriaId, cancellationToken);
        return categoria is not null && categoria.ComercioId == comercioId;
    }

    private async Task<bool> ExisteCodigoBarrasAsync(Guid comercioId, string? codigoBarras, Guid? excluirPresentacionId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(codigoBarras))
        {
            return false;
        }

        return await _productos.ExisteCodigoBarrasAsync(comercioId, codigoBarras.Trim(), excluirPresentacionId, cancellationToken);
    }

    private static ProductoResult ToResult(Producto producto) => new(
        producto.Id,
        producto.Nombre,
        producto.CategoriaId,
        producto.Activo,
        producto.Presentaciones.Select(ToResult).ToList());

    private static PresentacionResult ToResult(Presentacion presentacion) => new(
        presentacion.Id,
        presentacion.Nombre,
        presentacion.CodigoBarras,
        presentacion.PrecioVentaCentavos,
        presentacion.PrecioCostoCentavos,
        presentacion.Activa,
        presentacion.StockActual,
        presentacion.StockMinimo,
        presentacion.StockMinimo.HasValue && presentacion.StockActual <= presentacion.StockMinimo.Value);

}
