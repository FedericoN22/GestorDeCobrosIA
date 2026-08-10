using Kiosk.Application.Intenciones;
using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Catalogos;
using Kiosk.Domain.Common;

namespace Kiosk.Application.CasosUso.Whatsapp;

public enum EstadoResolucion
{
    NO_APLICA = 0,
    NO_ENCONTRADO = 1,
    AMBIGUO = 2,
    OK = 3
}

public sealed record ResultadoResolucion(
    EstadoResolucion Estado,
    CoincidenciaPresentacion? Coincidencia,
    IReadOnlyList<CoincidenciaPresentacion> Candidatos,
    string? Motivo);

public sealed record CoincidenciaPresentacion(Producto Producto, Presentacion Presentacion);

public sealed record ResultadoBusqueda
{
    public bool Buscado { get; init; }
    public bool ProductoEncontrado { get; init; }
    public bool BuscoPresentacion { get; init; }
    public string? PresentacionBuscada { get; init; }
    public IReadOnlyList<CoincidenciaPresentacion> Coincidencias { get; init; } = [];
}

public sealed class ResolvedorCatalogos
{
    private readonly IProductRepository _productos;

    public ResolvedorCatalogos(IProductRepository productos)
    {
        _productos = productos;
    }

    public async Task<ResultadoBusqueda> BuscarAsync(
        Guid comercioId,
        string? productoNombre,
        string? presentacionNombre,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(productoNombre))
        {
            return new ResultadoBusqueda { Buscado = false };
        }

        var norm = Normalizacion.Normalizar(productoNombre);
        var activos = await _productos.GetActivosAsync(comercioId, cancellationToken);

        var porNombre = activos.Where(p => Normalizacion.Normalizar(p.Nombre) == norm).ToList();
        var porContiene = porNombre.Count == 0
            ? activos.Where(p => Normalizacion.Normalizar(p.Nombre).Contains(norm)).ToList()
            : [];

        var candidatos = porNombre.Count > 0 ? porNombre : porContiene;

        var coincidencias = new List<CoincidenciaPresentacion>();
        foreach (var producto in candidatos)
        {
            var presentaciones = producto.Presentaciones.Where(p => p.Activa).ToList();

            if (!string.IsNullOrWhiteSpace(presentacionNombre))
            {
                var pNorm = Normalizacion.Normalizar(presentacionNombre);
                presentaciones = presentaciones
                    .Where(p =>
                    {
                        var n = Normalizacion.Normalizar(p.Nombre);
                        return n == pNorm || n.Contains(pNorm) || pNorm.Contains(n);
                    })
                    .ToList();
            }

            coincidencias.AddRange(presentaciones.Select(p => new CoincidenciaPresentacion(producto, p)));
        }

        return new ResultadoBusqueda
        {
            Buscado = true,
            ProductoEncontrado = candidatos.Count > 0,
            BuscoPresentacion = !string.IsNullOrWhiteSpace(presentacionNombre),
            PresentacionBuscada = presentacionNombre,
            Coincidencias = coincidencias
        };
    }

    public async Task<IReadOnlyList<Producto>> ListarActivosAsync(Guid comercioId, CancellationToken cancellationToken = default)
        => await _productos.GetActivosAsync(comercioId, cancellationToken);

    public async Task<ResultadoResolucion> ResolverAsync(
        Guid comercioId,
        StructuredCommand comando,
        CancellationToken cancellationToken = default)
    {
        if (comando.Accion is AccionIntencion.LISTAR_PRODUCTOS or AccionIntencion.CREAR_PRODUCTO)
        {
            return new ResultadoResolucion(EstadoResolucion.NO_APLICA, null, [], null);
        }

        var p = comando.Parametros;
        var busqueda = await BuscarAsync(comercioId, p.Producto, p.Presentacion, cancellationToken);

        if (!busqueda.Buscado)
        {
            return new ResultadoResolucion(EstadoResolucion.NO_ENCONTRADO, null, [], "Falta el nombre del producto.");
        }

        if (!busqueda.ProductoEncontrado)
        {
            return new ResultadoResolucion(
                EstadoResolucion.NO_ENCONTRADO,
                null,
                [],
                $"No encontré '{p.Producto}' en el catálogo.");
        }

        if (busqueda.Coincidencias.Count == 0)
        {
            var motivo = busqueda.BuscoPresentacion
                ? $"No encontré la presentación '{busqueda.PresentacionBuscada}' para '{p.Producto}'."
                : $"'{p.Producto}' no tiene presentaciones activas.";

            return new ResultadoResolucion(EstadoResolucion.NO_ENCONTRADO, null, [], motivo);
        }

        if (busqueda.Coincidencias.Count > 1)
        {
            return new ResultadoResolucion(
                EstadoResolucion.AMBIGUO,
                null,
                busqueda.Coincidencias,
                "El producto tiene más de una presentación; hay que aclarar cuál.");
        }

        return new ResultadoResolucion(EstadoResolucion.OK, busqueda.Coincidencias[0], [], null);
    }
}
