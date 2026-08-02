namespace Kiosk.Web.Models;

public sealed record LoginRequest(string Username, string Password);

public sealed record LoginResponse(string Token, DateTime ExpiraEn, UsuarioResponse Usuario);

public sealed record UsuarioResponse(
    Guid Id,
    string Username,
    string Nombre,
    string Rol,
    Guid ComercioId,
    IReadOnlyCollection<string> Permisos);

public sealed record CategoriaResponse(Guid Id, string Nombre, bool Activa);

public sealed record ProductoResponse(
    Guid Id,
    string Nombre,
    Guid? CategoriaId,
    bool Activo,
    IReadOnlyList<PresentacionResponse> Presentaciones);

public sealed record PresentacionResponse(
    Guid Id,
    string Nombre,
    string? CodigoBarras,
    int PrecioVentaCentavos,
    int? PrecioCostoCentavos,
    bool Activa,
    int StockActual,
    int? StockMinimo,
    bool StockBajo);

public sealed record CrearProductoRequest(string Nombre, Guid? CategoriaId);

public sealed record CrearProductoResponse(Guid ProductoId, Guid? PresentacionId);

public sealed record AgregarPresentacionRequest(string Nombre, int PrecioVentaCentavos, int? PrecioCostoCentavos, string? CodigoBarras);

public sealed record EntradaStockRequest(Guid PresentacionId, int Cantidad, int? PrecioCostoCentavos);

public sealed record StockActualResponse(Guid PresentacionId, int StockActual, int? StockMinimo, bool StockBajo);

public sealed record ErrorResponse(string? Error, string? Message);

public static class Moneda
{
    private static readonly System.Globalization.CultureInfo EsAr = System.Globalization.CultureInfo.GetCultureInfo("es-AR");

    public static int Acentavos(decimal pesos) => (int)Math.Round(pesos * 100m, MidpointRounding.AwayFromZero);

    public static decimal DeCentavos(int centavos) => centavos / 100m;

    public static string Formatear(int centavos) => "$" + DeCentavos(centavos).ToString("N2", EsAr);
}
