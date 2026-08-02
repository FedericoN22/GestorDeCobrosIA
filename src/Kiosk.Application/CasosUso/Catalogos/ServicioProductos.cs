using Kiosk.Application.Abstractions;
using Kiosk.Application.Puertos;
using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Catalogos;
using Kiosk.Domain.Common;

namespace Kiosk.Application.CasosUso.Catalogos;

public sealed record CrearProductoCommand(Guid ComercioId, Guid? CategoriaId, string Nombre);

public sealed record AgregarPresentacionCommand(
    Guid ComercioId,
    Guid ProductoId,
    string Nombre,
    int PrecioVentaCentavos,
    int? PrecioCostoCentavos = null,
    string? CodigoBarras = null);

public sealed record CrearProductoResult(Guid ProductoId, Guid? PresentacionId);

public sealed class ServicioProductos
{
    private readonly IProductRepository _productos;
    private readonly IUnitOfWork _unitOfWork;

    public ServicioProductos(IProductRepository productos, IUnitOfWork unitOfWork)
    {
        _productos = productos;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CrearProductoResult>> CrearProductoAsync(
        CrearProductoCommand command,
        CancellationToken cancellationToken = default)
    {
        var nombreNormalizado = Normalizacion.Normalizar(command.Nombre);
        if (await _productos.ExisteNombreAsync(command.ComercioId, nombreNormalizado, cancellationToken))
        {
            return Result<CrearProductoResult>.Fail(
                new Error("PRODUCTO_DUPLICADO", $"Ya existe un producto llamado '{command.Nombre}'."));
        }

        var producto = Producto.Crear(command.ComercioId, command.CategoriaId, command.Nombre);
        _productos.Add(producto);
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

        var presentacion = producto.AgregarPresentacion(
            command.Nombre,
            command.PrecioVentaCentavos,
            command.PrecioCostoCentavos,
            command.CodigoBarras);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Ok(presentacion.Id);
    }
}
