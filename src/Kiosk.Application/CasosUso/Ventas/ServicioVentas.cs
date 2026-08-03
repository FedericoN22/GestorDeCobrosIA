using Kiosk.Application.Abstractions;
using Kiosk.Application.Auditoria;
using Kiosk.Application.Puertos;
using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Auditoria;
using Kiosk.Domain.Catalogos;
using Kiosk.Domain.Common;
using Kiosk.Domain.Stock;
using Kiosk.Domain.Ventas;

namespace Kiosk.Application.CasosUso.Ventas;

public sealed record LineaVentaCommand(Guid PresentacionId, int Cantidad);

public sealed record PagoCommand(MedioPago Medio, int MontoCentavos);

public sealed record RegistrarVentaCommand(
    Guid ComercioId,
    Guid? UsuarioId,
    string Actor,
    Canal Origen,
    IReadOnlyList<LineaVentaCommand> Lineas,
    IReadOnlyList<PagoCommand> Pagos);

public sealed record RegistrarVentaResult(Guid VentaId, int Numero, int TotalCentavos, int VueltoCentavos);

public sealed class ServicioVentas
{
    private readonly ICajaRepository _cajas;
    private readonly IVentaRepository _ventas;
    private readonly IProductRepository _productos;
    private readonly IStockLedger _stockLedger;
    private readonly IAuditoriaRepository _auditoria;
    private readonly IUnitOfWork _unitOfWork;

    public ServicioVentas(
        ICajaRepository cajas,
        IVentaRepository ventas,
        IProductRepository productos,
        IStockLedger stockLedger,
        IAuditoriaRepository auditoria,
        IUnitOfWork unitOfWork)
    {
        _cajas = cajas;
        _ventas = ventas;
        _productos = productos;
        _stockLedger = stockLedger;
        _auditoria = auditoria;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RegistrarVentaResult>> RegistrarAsync(
        RegistrarVentaCommand command,
        CancellationToken cancellationToken = default)
    {
        var caja = await _cajas.GetActivaAsync(command.ComercioId, cancellationToken);
        if (caja is null)
        {
            return Result<RegistrarVentaResult>.Fail(new Error("CAJA_NO_ABIERTA", "No hay una caja abierta para registrar la venta."));
        }

        if (command.Lineas.Count == 0)
        {
            return Result<RegistrarVentaResult>.Fail(new Error("VENTA_SIN_LINEAS", "La venta debe tener al menos una línea."));
        }

        if (command.Pagos.Count == 0)
        {
            return Result<RegistrarVentaResult>.Fail(new Error("VENTA_SIN_PAGOS", "La venta debe tener al menos un pago."));
        }

        var numero = await _ventas.GetProximoNumeroAsync(command.ComercioId, cancellationToken);
        var venta = Venta.Crear(command.ComercioId, caja.Id, numero, DateTime.UtcNow, clientGenerated: command.Origen == Canal.WHATSAPP);

        var presentaciones = new Dictionary<Guid, Presentacion>();

        foreach (var linea in command.Lineas)
        {
            var producto = await _productos.GetByPresentacionIdAsync(linea.PresentacionId, cancellationToken);
            var presentacion = producto?.Presentaciones.FirstOrDefault(p => p.Id == linea.PresentacionId && p.Activa);
            if (producto is null || producto.ComercioId != command.ComercioId || presentacion is null)
            {
                return Result<RegistrarVentaResult>.Fail(
                    new Error("PRESENTACION_NO_ENCONTRADA", "Una presentación de la venta no existe o está desactivada."));
            }

            var stockActual = await _stockLedger.CalcularStockAsync(linea.PresentacionId, cancellationToken);
            if (stockActual < linea.Cantidad)
            {
                return Result<RegistrarVentaResult>.Fail(
                    new Error("STOCK_INSUFICIENTE", $"Stock insuficiente para '{presentacion.Nombre}' (disponible: {stockActual})."));
            }

            venta.AgregarLinea(
                presentacion.Id,
                producto.Nombre,
                presentacion.Nombre,
                linea.Cantidad,
                presentacion.PrecioVentaCentavos);
            presentaciones[presentacion.Id] = presentacion;
        }

        foreach (var pago in command.Pagos)
        {
            venta.AgregarPago(pago.Medio, pago.MontoCentavos);
        }

        venta.ValidarPagosCompletos();

        _ventas.Add(venta);
        foreach (var linea in venta.Lineas)
        {
            _stockLedger.Add(MovimientoStock.Venta(linea.PresentacionId, linea.Cantidad, venta.Id, command.Origen));
        }

        var vuelto = Math.Max(0, venta.TotalPagadoCentavos - venta.TotalCentavos);
        AuditoriaRegistrador.Registrar(
            _auditoria,
            command.ComercioId,
            command.Origen,
            command.Actor,
            AuditoriaTipos.VentaRegistrada,
            new
            {
                venta.Id,
                venta.Numero,
                venta.TotalCentavos,
                venta.CajaId,
                venta.Fecha,
                command.Origen,
                Lineas = venta.Lineas.Select(l => new { l.PresentacionId, l.Cantidad, l.PrecioUnitarioCentavos }),
                Pagos = venta.Pagos.Select(p => new { p.Medio, p.MontoCentavos })
            });
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var presentacion in presentaciones.Values)
        {
            var stock = await _stockLedger.CalcularStockAsync(presentacion.Id, cancellationToken);
            presentacion.ActualizarStock(stock);
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<RegistrarVentaResult>.Ok(
            new RegistrarVentaResult(venta.Id, venta.Numero, venta.TotalCentavos, vuelto));
    }

    public async Task<Venta?> ObtenerAsync(Guid comercioId, Guid ventaId, CancellationToken cancellationToken = default)
    {
        var venta = await _ventas.GetByIdAsync(ventaId, cancellationToken);
        return venta is not null && venta.ComercioId == comercioId ? venta : null;
    }
}
