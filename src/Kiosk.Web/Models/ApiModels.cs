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

public sealed record VentaPorDiaReporte(DateTime Fecha, int TotalCentavos, int Cantidad);

public sealed record VentaPorMedioPagoReporte(int Medio, int MontoCentavos, int Cantidad);

public sealed record VentaPorCajeroReporte(Guid UsuarioId, string CajeroNombre, int TotalCentavos, int Cantidad);

public sealed record ReporteVentasResponse(
    int TotalVendidoCentavos,
    int CantidadVentas,
    int TicketPromedioCentavos,
    IReadOnlyList<VentaPorDiaReporte> PorDia,
    IReadOnlyList<VentaPorMedioPagoReporte> PorMedioPago,
    IReadOnlyList<VentaPorCajeroReporte> PorCajero);

public sealed record CierreCajaReporte(
    Guid CajaId,
    Guid UsuarioId,
    string CajeroNombre,
    DateTime FechaApertura,
    DateTime? FechaCierre,
    int MontoInicialCentavos,
    int? MontoEsperadoCentavos,
    int? MontoDeclaradoCentavos,
    int? DiferenciaCentavos);

public sealed record MovimientoStockReporte(
    Guid MovimientoId,
    Guid PresentacionId,
    string ProductoNombre,
    string PresentacionNombre,
    int Tipo,
    int Cantidad,
    string? Motivo,
    Guid? VentaId,
    Guid? UsuarioId,
    string? UsuarioNombre,
    int Origen,
    DateTime CreatedAt,
    int StockActual,
    int? StockMinimo,
    bool StockBajo);

public sealed record GananciaPorProductoReporte(
    Guid PresentacionId,
    Guid ProductoId,
    string ProductoNombre,
    string PresentacionNombre,
    int Unidades,
    int IngresosCentavos,
    int CostoCentavos,
    int GananciaCentavos,
    decimal? MargenPct);

public sealed record ReporteGananciasResponse(
    int IngresosCentavos,
    int CostoCentavos,
    int GananciaCentavos,
    decimal? MargenPct,
    IReadOnlyList<GananciaPorProductoReporte> PorProducto);

public sealed record RankingItemReporte(
    Guid PresentacionId,
    string ProductoNombre,
    string PresentacionNombre,
    int Unidades,
    int IngresosCentavos,
    decimal PorcentajeDelTotal);

public sealed record ReporteRankingResponse(
    IReadOnlyList<RankingItemReporte> PorUnidades,
    IReadOnlyList<RankingItemReporte> PorIngresos);

public sealed record AuditoriaEventoReporte(
    long Id,
    DateTime Fecha,
    int Canal,
    string Actor,
    string Tipo,
    string? DetalleJson,
    Guid? IntencionId);

public static class Etiquetas
{
    public static string Medio(int medio) => medio switch
    {
        1 => "Efectivo",
        2 => "Tarjeta",
        _ => "Transferencia/QR"
    };

    public static string TipoMovimiento(int tipo) => tipo switch
    {
        1 => "Entrada",
        2 => "Ajuste",
        3 => "Venta",
        _ => "Devolución"
    };

    public static string Canal(int canal) => canal switch
    {
        1 => "POS",
        3 => "WhatsApp",
        _ => "Panel web"
    };
}

public static class Moneda
{
    private static readonly System.Globalization.CultureInfo EsAr = System.Globalization.CultureInfo.GetCultureInfo("es-AR");

    public static int Acentavos(decimal pesos) => (int)Math.Round(pesos * 100m, MidpointRounding.AwayFromZero);

    public static decimal DeCentavos(int centavos) => centavos / 100m;

    public static string Formatear(int centavos) => "$" + DeCentavos(centavos).ToString("N2", EsAr);
}
