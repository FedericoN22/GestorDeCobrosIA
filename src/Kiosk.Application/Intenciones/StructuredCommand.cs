namespace Kiosk.Application.Intenciones;

public enum AccionIntencion
{
    CONSULTAR_STOCK = 1,
    CONSULTAR_PRECIO = 2,
    LISTAR_PRODUCTOS = 3,
    AGREGAR_STOCK = 4,
    CREAR_PRODUCTO = 5,
    MODIFICAR_PRECIO = 6,
    ELIMINAR_PRODUCTO = 7
}

public enum TipoPrecio
{
    VENTA = 1,
    COSTO = 2,
    NO_INDICADO = 3
}

public sealed record ParametrosComando(
    string? Producto,
    string? Presentacion,
    int? Cantidad,
    int? Precio,
    TipoPrecio TipoPrecio,
    string? Categoria,
    string? Texto);

public sealed record StructuredCommand(
    int Version,
    AccionIntencion Accion,
    string Entidad,
    ParametrosComando Parametros,
    decimal Confianza,
    IReadOnlyList<string> CamposFaltantes,
    IReadOnlyList<string> CamposAmbiguos,
    string TextoOriginal)
{
    public bool EsDestructivo =>
        Accion == AccionIntencion.MODIFICAR_PRECIO || Accion == AccionIntencion.ELIMINAR_PRODUCTO;

    public bool TieneConfianzaSuficiente => Confianza >= 0.7m;

    public bool EsEjecutableSinAclaracion =>
        TieneConfianzaSuficiente && CamposFaltantes.Count == 0 && CamposAmbiguos.Count == 0;
}
