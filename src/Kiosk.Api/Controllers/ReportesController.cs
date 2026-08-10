using System.Text;
using Kiosk.Api.Reportes;
using Kiosk.Application.CasosUso.Reportes;
using Kiosk.Domain.Common;
using Kiosk.Domain.Stock;
using Kiosk.Domain.Usuarios;
using Kiosk.Domain.Ventas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kiosk.Api.Controllers;

[Route("api/reportes")]
public sealed class ReportesController : ApiControllerBase
{
    private readonly ServicioReportes _servicio;

    public ReportesController(ServicioReportes servicio)
    {
        _servicio = servicio;
    }

    [HttpGet("ventas")]
    [Authorize(Policy = Permisos.ReportesVer)]
    public async Task<ActionResult<ReporteVentas>> Ventas(DateTime? desde, DateTime? hasta, CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId)
        {
            return Unauthorized();
        }

        var (d, h) = Rango(desde, hasta);
        var resultado = await _servicio.VentasPorPeriodoAsync(comercioId, d, h, cancellationToken);
        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : ErrorResponse(resultado.Error!);
    }

    [HttpGet("ventas.csv")]
    [Authorize(Policy = Permisos.ReportesVer)]
    public async Task<IActionResult> VentasCsv(DateTime? desde, DateTime? hasta, CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId)
        {
            return Unauthorized();
        }

        var (d, h) = Rango(desde, hasta);
        var resultado = await _servicio.VentasPorPeriodoAsync(comercioId, d, h, cancellationToken);
        if (!resultado.IsSuccess)
        {
            return ErrorResponse(resultado.Error!);
        }

        var sb = new StringBuilder();
        sb.AppendLine("Fecha;Cantidad de ventas;Total");
        foreach (var fila in resultado.Value!.PorDia)
        {
            sb.AppendLine(CsvExportador.Fila(
                CsvExportador.FechaDia(fila.Fecha),
                fila.Cantidad.ToString(),
                CsvExportador.Monto(fila.TotalCentavos)));
        }

        return DescargarCsv(sb.ToString(), "ventas-por-periodo");
    }

    [HttpGet("cierres")]
    [Authorize(Policy = Permisos.ReportesVer)]
    public async Task<ActionResult<IReadOnlyList<CierreCajaReporte>>> Cierres(
        Guid? usuarioId,
        DateTime? desde,
        DateTime? hasta,
        bool soloDiferencias,
        CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId)
        {
            return Unauthorized();
        }

        var (d, h) = Rango(desde, hasta);
        var resultado = await _servicio.CierresAsync(comercioId, usuarioId, d, h, soloDiferencias, cancellationToken);
        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : ErrorResponse(resultado.Error!);
    }

    [HttpGet("cierres.csv")]
    [Authorize(Policy = Permisos.ReportesVer)]
    public async Task<IActionResult> CierresCsv(
        Guid? usuarioId,
        DateTime? desde,
        DateTime? hasta,
        bool soloDiferencias,
        CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId)
        {
            return Unauthorized();
        }

        var (d, h) = Rango(desde, hasta);
        var resultado = await _servicio.CierresAsync(comercioId, usuarioId, d, h, soloDiferencias, cancellationToken);
        if (!resultado.IsSuccess)
        {
            return ErrorResponse(resultado.Error!);
        }

        var sb = new StringBuilder();
        sb.AppendLine("Cajero;Apertura;Cierre;Monto inicial;Esperado;Declarado;Diferencia");
        foreach (var cierre in resultado.Value!)
        {
            sb.AppendLine(CsvExportador.Fila(
                cierre.CajeroNombre,
                CsvExportador.Fecha(cierre.FechaApertura),
                cierre.FechaCierre is { } fechaCierre ? CsvExportador.Fecha(fechaCierre) : null,
                CsvExportador.Monto(cierre.MontoInicialCentavos),
                cierre.MontoEsperadoCentavos is { } esperado ? CsvExportador.Monto(esperado) : null,
                cierre.MontoDeclaradoCentavos is { } declarado ? CsvExportador.Monto(declarado) : null,
                cierre.DiferenciaCentavos is { } diferencia ? CsvExportador.Monto(diferencia) : null));
        }

        return DescargarCsv(sb.ToString(), "cierres-de-caja");
    }

    [HttpGet("movimientos")]
    [Authorize(Policy = Permisos.ReportesVer)]
    public async Task<ActionResult<IReadOnlyList<MovimientoStockReporte>>> Movimientos(
        Guid? presentacionId,
        TipoMovimiento? tipo,
        Canal? origen,
        Guid? usuarioId,
        DateTime? desde,
        DateTime? hasta,
        CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId)
        {
            return Unauthorized();
        }

        var (d, h) = Rango(desde, hasta);
        var resultado = await _servicio.MovimientosAsync(
            comercioId,
            presentacionId,
            tipo,
            origen,
            usuarioId,
            d,
            h,
            cancellationToken);
        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : ErrorResponse(resultado.Error!);
    }

    [HttpGet("movimientos.csv")]
    [Authorize(Policy = Permisos.ReportesVer)]
    public async Task<IActionResult> MovimientosCsv(
        Guid? presentacionId,
        TipoMovimiento? tipo,
        Canal? origen,
        Guid? usuarioId,
        DateTime? desde,
        DateTime? hasta,
        CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId)
        {
            return Unauthorized();
        }

        var (d, h) = Rango(desde, hasta);
        var resultado = await _servicio.MovimientosAsync(
            comercioId,
            presentacionId,
            tipo,
            origen,
            usuarioId,
            d,
            h,
            cancellationToken);
        if (!resultado.IsSuccess)
        {
            return ErrorResponse(resultado.Error!);
        }

        var sb = new StringBuilder();
        sb.AppendLine("Fecha;Producto;Presentación;Tipo;Cantidad;Usuario;Origen;Motivo;Stock actual;Stock mínimo;Estado");
        foreach (var m in resultado.Value!)
        {
            sb.AppendLine(CsvExportador.Fila(
                CsvExportador.Fecha(m.CreatedAt),
                m.ProductoNombre,
                m.PresentacionNombre,
                EtiquetaTipo(m.Tipo),
                m.Cantidad.ToString(),
                m.UsuarioNombre,
                EtiquetaCanal(m.Origen),
                m.Motivo,
                m.StockActual.ToString(),
                m.StockMinimo?.ToString(),
                m.StockBajo ? "Stock bajo" : "OK"));
        }

        return DescargarCsv(sb.ToString(), "movimientos-de-stock");
    }

    [HttpGet("ganancias")]
    [Authorize(Policy = Permisos.GananciasVer)]
    public async Task<ActionResult<ReporteGanancias>> Ganancias(DateTime? desde, DateTime? hasta, CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId)
        {
            return Unauthorized();
        }

        var (d, h) = Rango(desde, hasta);
        var resultado = await _servicio.GananciasAsync(comercioId, d, h, cancellationToken);
        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : ErrorResponse(resultado.Error!);
    }

    [HttpGet("ganancias.csv")]
    [Authorize(Policy = Permisos.GananciasVer)]
    public async Task<IActionResult> GananciasCsv(DateTime? desde, DateTime? hasta, CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId)
        {
            return Unauthorized();
        }

        var (d, h) = Rango(desde, hasta);
        var resultado = await _servicio.GananciasAsync(comercioId, d, h, cancellationToken);
        if (!resultado.IsSuccess)
        {
            return ErrorResponse(resultado.Error!);
        }

        var sb = new StringBuilder();
        sb.AppendLine("Producto;Presentación;Unidades;Ingresos;Costo;Ganancia;Margen %");
        foreach (var fila in resultado.Value!.PorProducto)
        {
            sb.AppendLine(CsvExportador.Fila(
                fila.ProductoNombre,
                fila.PresentacionNombre,
                fila.Unidades.ToString(),
                CsvExportador.Monto(fila.IngresosCentavos),
                CsvExportador.Monto(fila.CostoCentavos),
                CsvExportador.Monto(fila.GananciaCentavos),
                fila.MargenPct?.ToString("N2") ?? "—"));
        }

        return DescargarCsv(sb.ToString(), "ganancias");
    }

    [HttpGet("ranking")]
    [Authorize(Policy = Permisos.GananciasVer)]
    public async Task<ActionResult<ReporteRanking>> Ranking(DateTime? desde, DateTime? hasta, int? top, CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId)
        {
            return Unauthorized();
        }

        var (d, h) = Rango(desde, hasta);
        var topN = Math.Clamp(top ?? 10, 1, 100);
        var resultado = await _servicio.RankingAsync(comercioId, d, h, topN, cancellationToken);
        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : ErrorResponse(resultado.Error!);
    }

    [HttpGet("ranking.csv")]
    [Authorize(Policy = Permisos.GananciasVer)]
    public async Task<IActionResult> RankingCsv(DateTime? desde, DateTime? hasta, int? top, CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId)
        {
            return Unauthorized();
        }

        var (d, h) = Rango(desde, hasta);
        var topN = Math.Clamp(top ?? 10, 1, 100);
        var resultado = await _servicio.RankingAsync(comercioId, d, h, topN, cancellationToken);
        if (!resultado.IsSuccess)
        {
            return ErrorResponse(resultado.Error!);
        }

        var sb = new StringBuilder();
        sb.AppendLine("Producto;Presentación;Unidades;Ingresos;% del total");
        foreach (var fila in resultado.Value!.PorUnidades)
        {
            sb.AppendLine(CsvExportador.Fila(
                fila.ProductoNombre,
                fila.PresentacionNombre,
                fila.Unidades.ToString(),
                CsvExportador.Monto(fila.IngresosCentavos),
                fila.PorcentajeDelTotal.ToString("N2")));
        }

        return DescargarCsv(sb.ToString(), "ranking-de-productos");
    }

    [HttpGet("auditoria")]
    [Authorize(Policy = Permisos.AuditoriaVer)]
    public async Task<ActionResult<IReadOnlyList<AuditoriaEventoReporte>>> Auditoria(
        Canal? canal,
        string? actor,
        string? tipo,
        DateTime? desde,
        DateTime? hasta,
        CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId)
        {
            return Unauthorized();
        }

        var (d, h) = Rango(desde, hasta);
        var resultado = await _servicio.AuditoriaAsync(comercioId, canal, actor, tipo, d, h, cancellationToken);
        return resultado.IsSuccess
            ? Ok(resultado.Value)
            : ErrorResponse(resultado.Error!);
    }

    [HttpGet("auditoria.csv")]
    [Authorize(Policy = Permisos.AuditoriaVer)]
    public async Task<IActionResult> AuditoriaCsv(
        Canal? canal,
        string? actor,
        string? tipo,
        DateTime? desde,
        DateTime? hasta,
        CancellationToken cancellationToken)
    {
        if (ComercioId is not Guid comercioId)
        {
            return Unauthorized();
        }

        var (d, h) = Rango(desde, hasta);
        var resultado = await _servicio.AuditoriaAsync(comercioId, canal, actor, tipo, d, h, cancellationToken);
        if (!resultado.IsSuccess)
        {
            return ErrorResponse(resultado.Error!);
        }

        var sb = new StringBuilder();
        sb.AppendLine("Fecha;Canal;Actor;Tipo;Detalle");
        foreach (var evento in resultado.Value!)
        {
            sb.AppendLine(CsvExportador.Fila(
                CsvExportador.Fecha(evento.Fecha),
                EtiquetaCanal(evento.Canal),
                evento.Actor,
                evento.Tipo,
                evento.DetalleJson));
        }

        return DescargarCsv(sb.ToString(), "auditoria");
    }

    private IActionResult DescargarCsv(string contenido, string nombreArchivo)
        => File(CsvExportador.Bytes(contenido), "text/csv; charset=utf-8", $"{nombreArchivo}.csv");

    private static (DateTime Desde, DateTime Hasta) Rango(DateTime? desde, DateTime? hasta)
    {
        var hoy = DateTime.Today;
        return (Utc(desde?.Date ?? hoy), Utc(hasta?.Date ?? hoy));
    }

    private static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static string EtiquetaTipo(TipoMovimiento tipo) => tipo switch
    {
        TipoMovimiento.ENTRADA_MANUAL => "Entrada",
        TipoMovimiento.AJUSTE => "Ajuste",
        TipoMovimiento.VENTA => "Venta",
        _ => "Devolución"
    };

    private static string EtiquetaCanal(Canal canal) => canal switch
    {
        Canal.POS => "POS",
        Canal.WHATSAPP => "WhatsApp",
        _ => "Panel web"
    };
}
