using Kiosk.Application.Abstractions;
using Kiosk.Application.Puertos;
using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Catalogos;
using Kiosk.Domain.Common;
using Kiosk.Domain.Stock;
using Kiosk.Domain.Ventas;

namespace Kiosk.Application.CasosUso.Ventas;

public sealed record LineaVentaCommand(Guid PresentacionId, int Cantidad);

public sealed record PagoCommand(MedioPago Medio, int MontoCentavos);

public sealed record RegistrarVentaCommand(
    Guid ComercioId,
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
    private readonly IUnitOfWork _unitOfWork;

    public ServicioVentas(
        ICajaRepository cajas,
        IVentaRepository ventas,
        IProductRepository productos,
        IStockLedger stockLedger,
        IUnitOfWork unitOfWork)
    {
        _cajas = cajas;
        _ventas = ventas;
        _productos = productos;
        _stockLedger = stockLedger;
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

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var vuelto = Math.Max(0, venta.TotalPagadoCentavos - venta.TotalCentavos);
        return Result<RegistrarVentaResult>.Ok(
            new RegistrarVentaResult(venta.Id, venta.Numero, venta.TotalCentavos, vuelto));
    }
}
