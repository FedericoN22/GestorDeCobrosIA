using Kiosk.Application.Abstractions;
using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Common;
using Kiosk.Domain.Stock;
using Kiosk.Domain.Ventas;

namespace Kiosk.Application.CasosUso.Reportes;

public sealed record VentaPorDia(DateTime Fecha, int TotalCentavos, int Cantidad);

public sealed record VentaPorMedioPago(MedioPago Medio, int MontoCentavos, int Cantidad);

public sealed record VentaPorCajero(Guid UsuarioId, string CajeroNombre, int TotalCentavos, int Cantidad);

public sealed record ReporteVentas(
    int TotalVendidoCentavos,
    int CantidadVentas,
    int TicketPromedioCentavos,
    IReadOnlyList<VentaPorDia> PorDia,
    IReadOnlyList<VentaPorMedioPago> PorMedioPago,
    IReadOnlyList<VentaPorCajero> PorCajero);

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
    TipoMovimiento Tipo,
    int Cantidad,
    string? Motivo,
    Guid? VentaId,
    Guid? UsuarioId,
    string? UsuarioNombre,
    Canal Origen,
    DateTime CreatedAt,
    int StockActual,
    int? StockMinimo,
    bool StockBajo);

public sealed record GananciaPorProducto(
    Guid PresentacionId,
    Guid ProductoId,
    string ProductoNombre,
    string PresentacionNombre,
    int Unidades,
    int IngresosCentavos,
    int CostoCentavos,
    int GananciaCentavos,
    decimal? MargenPct);

public sealed record ReporteGanancias(
    int IngresosCentavos,
    int CostoCentavos,
    int GananciaCentavos,
    decimal? MargenPct,
    IReadOnlyList<GananciaPorProducto> PorProducto);

public sealed record RankingItem(
    Guid PresentacionId,
    string ProductoNombre,
    string PresentacionNombre,
    int Unidades,
    int IngresosCentavos,
    decimal PorcentajeDelTotal);

public sealed record ReporteRanking(
    IReadOnlyList<RankingItem> PorUnidades,
    IReadOnlyList<RankingItem> PorIngresos);

public sealed record AuditoriaEventoReporte(
    long Id,
    DateTime Fecha,
    Canal Canal,
    string Actor,
    string Tipo,
    string? DetalleJson,
    Guid? IntencionId);

public sealed class ServicioReportes
{
    private readonly IVentaRepository _ventas;
    private readonly ICajaRepository _cajas;
    private readonly IProductRepository _productos;
    private readonly IStockLedger _stockLedger;
    private readonly IAuditoriaRepository _auditoria;
    private readonly IUsuarioRepository _usuarios;

    public ServicioReportes(
        IVentaRepository ventas,
        ICajaRepository cajas,
        IProductRepository productos,
        IStockLedger stockLedger,
        IAuditoriaRepository auditoria,
        IUsuarioRepository usuarios)
    {
        _ventas = ventas;
        _cajas = cajas;
        _productos = productos;
        _stockLedger = stockLedger;
        _auditoria = auditoria;
        _usuarios = usuarios;
    }

    public async Task<Result<ReporteVentas>> VentasPorPeriodoAsync(
        Guid comercioId,
        DateTime desde,
        DateTime hasta,
        CancellationToken cancellationToken = default)
    {
        var ventas = await _ventas.ObtenerEnRangoAsync(comercioId, desde, HastaExclusivo(hasta), cancellationToken);

        if (ventas.Count == 0)
        {
            return Result<ReporteVentas>.Ok(new ReporteVentas(0, 0, 0, [], [], []));
        }

        var total = ventas.Sum(v => v.TotalCentavos);
        var cantidad = ventas.Count;
        var ticketPromedio = (int)Math.Round(total / (double)cantidad);

        var porDia = ventas
            .GroupBy(v => v.Fecha.Date)
            .Select(g => new VentaPorDia(g.Key, g.Sum(v => v.TotalCentavos), g.Count()))
            .OrderBy(x => x.Fecha)
            .ToList();

        var porMedioPago = ventas
            .SelectMany(v => v.Pagos)
            .GroupBy(p => p.Medio)
            .Select(g => new VentaPorMedioPago(g.Key, g.Sum(p => p.MontoCentavos), g.Count()))
            .OrderBy(x => x.Medio)
            .ToList();

        var cajas = await _cajas.ObtenerPorIdsAsync(ventas.Select(v => v.CajaId).Distinct(), cancellationToken);
        var usuarios = await _usuarios.ObtenerPorIdsAsync(cajas.Select(c => c.UsuarioId).Distinct(), cancellationToken);

        var porCajero = ventas
            .GroupBy(v => cajas.FirstOrDefault(c => c.Id == v.CajaId)?.UsuarioId ?? Guid.Empty)
            .Select(g => new VentaPorCajero(
                g.Key,
                usuarios.TryGetValue(g.Key, out var usuario) ? usuario.Nombre : "—",
                g.Sum(v => v.TotalCentavos),
                g.Count()))
            .OrderByDescending(x => x.TotalCentavos)
            .ToList();

        return Result<ReporteVentas>.Ok(
            new ReporteVentas(total, cantidad, ticketPromedio, porDia, porMedioPago, porCajero));
    }

    public async Task<Result<IReadOnlyList<CierreCajaReporte>>> CierresAsync(
        Guid comercioId,
        Guid? usuarioId,
        DateTime desde,
        DateTime hasta,
        bool soloDiferencias,
        CancellationToken cancellationToken = default)
    {
        var cajas = await _cajas.ObtenerCerradasAsync(
            comercioId,
            usuarioId,
            desde,
            HastaExclusivo(hasta),
            soloDiferencias,
            cancellationToken);

        if (cajas.Count == 0)
        {
            return Result<IReadOnlyList<CierreCajaReporte>>.Ok([]);
        }

        var usuarios = await _usuarios.ObtenerPorIdsAsync(cajas.Select(c => c.UsuarioId).Distinct(), cancellationToken);

        var lista = cajas
            .Select(c => new CierreCajaReporte(
                c.Id,
                c.UsuarioId,
                usuarios.TryGetValue(c.UsuarioId, out var usuario) ? usuario.Nombre : "—",
                c.FechaApertura,
                c.FechaCierre,
                c.MontoInicialCentavos,
                c.MontoEsperadoCentavos,
                c.MontoDeclaradoCentavos,
                c.DiferenciaCentavos))
            .ToList();

        return Result<IReadOnlyList<CierreCajaReporte>>.Ok(lista);
    }

    public async Task<Result<IReadOnlyList<MovimientoStockReporte>>> MovimientosAsync(
        Guid comercioId,
        Guid? presentacionId,
        TipoMovimiento? tipo,
        Canal? origen,
        Guid? usuarioId,
        DateTime desde,
        DateTime hasta,
        CancellationToken cancellationToken = default)
    {
        var movimientos = await _stockLedger.ObtenerEnRangoAsync(
            comercioId,
            presentacionId,
            tipo,
            origen,
            usuarioId,
            desde,
            HastaExclusivo(hasta),
            cancellationToken);

        if (movimientos.Count == 0)
        {
            return Result<IReadOnlyList<MovimientoStockReporte>>.Ok([]);
        }

        var productos = await _productos.GetTodosAsync(comercioId, cancellationToken);
        var presentacionesPorId = productos
            .SelectMany(p => p.Presentaciones)
            .ToDictionary(pr => pr.Id);
        var usuarios = await _usuarios.ObtenerPorIdsAsync(
            movimientos.Where(m => m.UsuarioId.HasValue).Select(m => m.UsuarioId!.Value).Distinct(),
            cancellationToken);
        var stocks = await _stockLedger.CalcularStockPorIdsAsync(
            movimientos.Select(m => m.PresentacionId).Distinct(),
            cancellationToken);

        var lista = movimientos
            .Select(m =>
            {
                presentacionesPorId.TryGetValue(m.PresentacionId, out var presentacion);
                var productoNombre = presentacion is null
                    ? "—"
                    : productos.First(p => p.Id == presentacion.ProductoId).Nombre;
                var stockActual = presentacion is not null && stocks.TryGetValue(presentacion.Id, out var stock)
                    ? stock
                    : 0;
                var stockMinimo = presentacion?.StockMinimo;
                var stockBajo = presentacion is not null && stockMinimo.HasValue && stockActual <= stockMinimo.Value;

                return new MovimientoStockReporte(
                    m.Id,
                    m.PresentacionId,
                    productoNombre,
                    presentacion?.Nombre ?? "—",
                    m.Tipo,
                    m.Cantidad,
                    m.Motivo,
                    m.VentaId,
                    m.UsuarioId,
                    m.UsuarioId is { } usuarioIdMov && usuarios.TryGetValue(usuarioIdMov, out var usuario)
                        ? usuario.Nombre
                        : null,
                    m.Origen,
                    m.CreatedAt,
                    stockActual,
                    stockMinimo,
                    stockBajo);
            })
            .ToList();

        return Result<IReadOnlyList<MovimientoStockReporte>>.Ok(lista);
    }

    public async Task<Result<ReporteGanancias>> GananciasAsync(
        Guid comercioId,
        DateTime desde,
        DateTime hasta,
        CancellationToken cancellationToken = default)
    {
        var lineas = await _ventas.ObtenerLineasEnRangoAsync(comercioId, desde, HastaExclusivo(hasta), cancellationToken);
        var productos = await _productos.GetTodosAsync(comercioId, cancellationToken);
        var presentacionesPorId = productos
            .SelectMany(p => p.Presentaciones)
            .ToDictionary(pr => pr.Id);

        int CostoDeLinea(LineaVenta linea)
        {
            if (presentacionesPorId.TryGetValue(linea.PresentacionId, out var presentacion)
                && presentacion.PrecioCostoCentavos is { } costo)
            {
                return linea.Cantidad * costo;
            }

            return 0;
        }

        var vendidas = lineas
            .GroupBy(l => l.PresentacionId)
            .Select(g => new
            {
                PresentacionId = g.Key,
                Unidades = g.Sum(l => l.Cantidad),
                Ingresos = g.Sum(l => l.SubtotalCentavos),
                Costo = g.Sum(CostoDeLinea)
            })
            .ToDictionary(x => x.PresentacionId);

        var quietas = presentacionesPorId.Values
            .Where(pr => pr.PrecioCostoCentavos.HasValue && !vendidas.ContainsKey(pr.Id));

        var porProducto = vendidas
            .Select(kv =>
            {
                var presentacion = presentacionesPorId[kv.Key];
                var ganancia = kv.Value.Ingresos - kv.Value.Costo;
                return new GananciaPorProducto(
                    kv.Key,
                    presentacion.ProductoId,
                    ProductoNombre(productos, presentacion.ProductoId),
                    presentacion.Nombre,
                    kv.Value.Unidades,
                    kv.Value.Ingresos,
                    kv.Value.Costo,
                    ganancia,
                    Margen(kv.Value.Ingresos, ganancia));
            })
            .Concat(quietas.Select(pr => new GananciaPorProducto(
                pr.Id,
                pr.ProductoId,
                ProductoNombre(productos, pr.ProductoId),
                pr.Nombre,
                0,
                0,
                0,
                0,
                null)))
            .OrderByDescending(x => x.GananciaCentavos)
            .ToList();

        var ingresos = lineas.Sum(l => l.SubtotalCentavos);
        var costo = lineas.Sum(CostoDeLinea);
        var ganancia = ingresos - costo;

        return Result<ReporteGanancias>.Ok(
            new ReporteGanancias(ingresos, costo, ganancia, Margen(ingresos, ganancia), porProducto));
    }

    public async Task<Result<ReporteRanking>> RankingAsync(
        Guid comercioId,
        DateTime desde,
        DateTime hasta,
        int topN,
        CancellationToken cancellationToken = default)
    {
        var lineas = await _ventas.ObtenerLineasEnRangoAsync(comercioId, desde, HastaExclusivo(hasta), cancellationToken);

        if (lineas.Count == 0)
        {
            return Result<ReporteRanking>.Ok(new ReporteRanking([], []));
        }

        var totalIngresos = lineas.Sum(l => l.SubtotalCentavos);
        var filas = lineas
            .GroupBy(l => l.PresentacionId)
            .Select(g =>
            {
                var primera = g.First();
                var ingresos = g.Sum(l => l.SubtotalCentavos);
                var porcentaje = totalIngresos > 0 ? ingresos / (decimal)totalIngresos * 100m : 0m;
                return new RankingItem(
                    g.Key,
                    primera.ProductoNombre,
                    primera.PresentacionNombre,
                    g.Sum(l => l.Cantidad),
                    ingresos,
                    Math.Round(porcentaje, 2));
            })
            .ToList();

        var porUnidades = filas.OrderByDescending(x => x.Unidades).ThenByDescending(x => x.IngresosCentavos).Take(topN).ToList();
        var porIngresos = filas.OrderByDescending(x => x.IngresosCentavos).ThenByDescending(x => x.Unidades).Take(topN).ToList();

        return Result<ReporteRanking>.Ok(new ReporteRanking(porUnidades, porIngresos));
    }

    public async Task<Result<IReadOnlyList<AuditoriaEventoReporte>>> AuditoriaAsync(
        Guid comercioId,
        Canal? canal,
        string? actor,
        string? tipo,
        DateTime desde,
        DateTime hasta,
        CancellationToken cancellationToken = default)
    {
        var eventos = await _auditoria.ObtenerEnRangoAsync(
            comercioId,
            canal,
            actor,
            tipo,
            desde,
            HastaExclusivo(hasta),
            cancellationToken);

        var reporte = eventos
            .Select(e => new AuditoriaEventoReporte(
                e.Id,
                e.CreatedAt,
                e.Canal,
                e.Actor,
                e.Tipo,
                e.DetalleJson,
                e.IntencionId))
            .ToList();

        return Result<IReadOnlyList<AuditoriaEventoReporte>>.Ok(reporte);
    }

    private static DateTime HastaExclusivo(DateTime hasta) => hasta.Date.AddDays(1);

    private static string ProductoNombre(IEnumerable<Domain.Catalogos.Producto> productos, Guid productoId)
        => productos.First(p => p.Id == productoId).Nombre;

    private static decimal? Margen(int ingresos, int ganancia)
        => ingresos > 0 ? Math.Round(ganancia / (decimal)ingresos * 100m, 2) : null;
}
