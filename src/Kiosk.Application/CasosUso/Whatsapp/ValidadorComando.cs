using Kiosk.Application.Intenciones;

namespace Kiosk.Application.CasosUso.Whatsapp;

public static class ValidadorComando
{
    public static IReadOnlyList<string> CalcularFaltantes(StructuredCommand comando)
    {
        var p = comando.Parametros;
        return comando.Accion switch
        {
            AccionIntencion.CONSULTAR_STOCK or AccionIntencion.CONSULTAR_PRECIO =>
                Requeridos(faltante("producto", string.IsNullOrWhiteSpace(p.Producto))),

            AccionIntencion.AGREGAR_STOCK =>
                Requeridos(
                    faltante("producto", string.IsNullOrWhiteSpace(p.Producto)),
                    faltante("presentacion", string.IsNullOrWhiteSpace(p.Presentacion)),
                    faltante("cantidad", !p.Cantidad.HasValue || p.Cantidad.Value <= 0)),

            AccionIntencion.CREAR_PRODUCTO =>
                Requeridos(
                    faltante("producto", string.IsNullOrWhiteSpace(p.Producto)),
                    faltante("precio", !string.IsNullOrWhiteSpace(p.Presentacion) && p.Precio is not > 0)),

            AccionIntencion.MODIFICAR_PRECIO =>
                Requeridos(
                    faltante("producto", string.IsNullOrWhiteSpace(p.Producto)),
                    faltante("presentacion", string.IsNullOrWhiteSpace(p.Presentacion)),
                    faltante("precio", p.Precio is not > 0)),

            AccionIntencion.ELIMINAR_PRODUCTO =>
                Requeridos(faltante("producto", string.IsNullOrWhiteSpace(p.Producto))),

            _ => []
        };
    }

    private static List<string> Requeridos(params string?[] valores)
        => valores.Where(v => v is not null).Cast<string>().ToList();

    private static string? faltante(string nombre, bool falta)
        => falta ? nombre : null;
}
