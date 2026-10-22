using Kiosk.Application.Intenciones;

namespace Kiosk.Application.CasosUso.Whatsapp;

public sealed class VerificadorEfectoAplicado
{
    private readonly ResolvedorCatalogos _resolvedor;

    public VerificadorEfectoAplicado(ResolvedorCatalogos resolvedor)
    {
        _resolvedor = resolvedor;
    }

    public async Task<bool> YaAplicadoAsync(
        Guid comercioId,
        StructuredCommand comando,
        CancellationToken cancellationToken = default)
    {
        var resolucion = await _resolvedor.ResolverAsync(comercioId, comando, cancellationToken);

        return comando.Accion switch
        {
            AccionIntencion.ELIMINAR_PRODUCTO => resolucion.Estado == EstadoResolucion.NO_ENCONTRADO,
            AccionIntencion.MODIFICAR_PRECIO => resolucion.Estado == EstadoResolucion.OK
                && resolucion.Coincidencia is not null
                && PrecioCoincide(resolucion.Coincidencia.Presentacion.PrecioVentaCentavos, comando.Parametros.Precio),
            _ => false
        };
    }

    private static bool PrecioCoincide(int precioVentaCentavos, int? precioEnPesos)
    {
        if (precioEnPesos is null || precioEnPesos > int.MaxValue / 100)
        {
            return false;
        }

        return precioVentaCentavos == precioEnPesos.Value * 100;
    }
}
