using System.Text;
using Kiosk.Domain.Common;
using Kiosk.Domain.Ventas;
using Kiosk.Pos.Models;

namespace Kiosk.Pos.Services;

public sealed class LineaCarrito
{
    public required Guid PresentacionId { get; init; }
    public required string ProductoNombre { get; init; }
    public required string PresentacionNombre { get; init; }
    public required int PrecioUnitarioCentavos { get; init; }
    public required int Cantidad { get; set; }
    public int SubtotalCentavos => Cantidad * PrecioUnitarioCentavos;
}

public sealed record CobroInfo(int EfectivoRecibidoCentavos, int TarjetaCentavos, int QrCentavos);

public sealed record ResultadoCobro(VentaLocal Venta, int VueltoCentavos);

public sealed record ResultadoCierreCaja(
    CajaLocal Caja,
    IReadOnlyDictionary<MedioPago, int> EsperadoPorMedio,
    int TotalEsperadoCentavos,
    int TotalDeclaradoCentavos,
    int DiferenciaCentavos);

public sealed class GestorVentas
{
    private readonly AlmacenLocal _almacen;
    private readonly SesionManager _sesiones;
    private readonly SyncEngine _sync;
    private readonly IPosPrinter _impresora;

    public GestorVentas(AlmacenLocal almacen, SesionManager sesiones, SyncEngine sync, IPosPrinter impresora)
    {
        _almacen = almacen;
        _sesiones = sesiones;
        _sync = sync;
        _impresora = impresora;
    }

    public List<LineaCarrito> Carrito { get; } = [];

    public int TotalCentavos => Carrito.Sum(l => l.SubtotalCentavos);

    public int TotalPagadoCentavos => Carrito.Sum(l => l.SubtotalCentavos);

    public CajaLocal? CajaActiva => _almacen.ObtenerCajaActiva();

    public IReadOnlyList<MedioPago> MediosDisponibles { get; } =
        [MedioPago.EFECTIVO, MedioPago.TARJETA, MedioPago.TRANSFERENCIA_QR];

    public void AgregarAlCarrito(ResultadoBusqueda producto, int cantidad)
    {
        if (cantidad <= 0)
        {
            throw new DomainException("CANTIDAD_INVALIDA", "La cantidad debe ser mayor a cero.");
        }

        var enCarrito = Carrito.FirstOrDefault(l => l.PresentacionId == producto.PresentacionId);
        var cantidadNueva = (enCarrito?.Cantidad ?? 0) + cantidad;
        var stockDisponible = _almacen.ObtenerStockLocal(producto.PresentacionId);

        if (cantidadNueva > stockDisponible)
        {
            throw new DomainException(
                "STOCK_INSUFICIENTE",
                $"Stock insuficiente para '{producto.PresentacionNombre}' (disponible: {stockDisponible}).");
        }

        if (enCarrito is not null)
        {
            enCarrito.Cantidad = cantidadNueva;
        }
        else
        {
            Carrito.Add(new LineaCarrito
            {
                PresentacionId = producto.PresentacionId,
                ProductoNombre = producto.ProductoNombre,
                PresentacionNombre = producto.PresentacionNombre,
                PrecioUnitarioCentavos = producto.PrecioVentaCentavos,
                Cantidad = cantidad
            });
        }
    }

    public void CambiarCantidad(int indice, int cantidad)
    {
        if (indice < 0 || indice >= Carrito.Count)
        {
            return;
        }

        if (cantidad <= 0)
        {
            Carrito.RemoveAt(indice);
            return;
        }

        var linea = Carrito[indice];
        var stock = _almacen.ObtenerStockLocal(linea.PresentacionId);
        if (cantidad > stock)
        {
            throw new DomainException("STOCK_INSUFICIENTE", $"Stock insuficiente (disponible: {stock}).");
        }

        linea.Cantidad = cantidad;
    }

    public void QuitarDelCarrito(int indice)
    {
        if (indice >= 0 && indice < Carrito.Count)
        {
            Carrito.RemoveAt(indice);
        }
    }

    public ResultadoCobro Cobrar(CobroInfo cobro)
    {
        var sesion = _sesiones.Actual ?? throw new DomainException("SESION_REQUERIDA", "No hay sesión iniciada.");
        var caja = CajaActiva ?? throw new DomainException("CAJA_NO_ABIERTA", "No hay una caja abierta.");
        if (Carrito.Count == 0)
        {
            throw new DomainException("VENTA_SIN_LINEAS", "El carrito está vacío.");
        }

        var total = TotalCentavos;
        var otrosMedios = cobro.TarjetaCentavos + cobro.QrCentavos;
        if (otrosMedios > total)
        {
            throw new DomainException("PAGO_EXCEDE_TOTAL", "El monto de tarjeta/QR supera el total de la venta.");
        }

        var restante = total - otrosMedios;
        if (cobro.EfectivoRecibidoCentavos < restante)
        {
            throw new DomainException("PAGO_INSUFICIENTE", "El efectivo recibido no alcanza para cubrir la venta.");
        }

        var venta = new VentaLocal
        {
            Id = Guid.NewGuid(),
            Numero = _almacen.SiguienteNumeroVenta(),
            CajaId = caja.Id,
            TotalCentavos = total,
            Fecha = DateTime.UtcNow,
            ClientGenerated = true,
            Lineas = Carrito.Select(l => new LineaLocal
            {
                PresentacionId = l.PresentacionId,
                ProductoNombre = l.ProductoNombre,
                PresentacionNombre = l.PresentacionNombre,
                Cantidad = l.Cantidad,
                PrecioUnitarioCentavos = l.PrecioUnitarioCentavos
            }).ToList()
        };

        if (cobro.EfectivoRecibidoCentavos > 0)
        {
            venta.Pagos.Add(new PagoLocal { Medio = MedioPago.EFECTIVO, MontoCentavos = cobro.EfectivoRecibidoCentavos });
        }

        if (cobro.TarjetaCentavos > 0)
        {
            venta.Pagos.Add(new PagoLocal { Medio = MedioPago.TARJETA, MontoCentavos = cobro.TarjetaCentavos });
        }

        if (cobro.QrCentavos > 0)
        {
            venta.Pagos.Add(new PagoLocal { Medio = MedioPago.TRANSFERENCIA_QR, MontoCentavos = cobro.QrCentavos });
        }

        var vuelto = cobro.EfectivoRecibidoCentavos - restante;

        _almacen.GuardarVenta(venta);
        foreach (var linea in venta.Lineas)
        {
            _almacen.DecrementarStock(linea.PresentacionId, linea.Cantidad, venta.Id);
        }

        _almacen.EncolarOperacion("VENTA", new VentaPayload
        {
            VentaId = venta.Id,
            CajaId = venta.CajaId,
            Numero = venta.Numero,
            Fecha = venta.Fecha,
            ClientGenerated = venta.ClientGenerated,
            Lineas = venta.Lineas.Select(l => new LineaVentaPayload
            {
                PresentacionId = l.PresentacionId,
                Cantidad = l.Cantidad,
                ProductoNombre = l.ProductoNombre,
                PresentacionNombre = l.PresentacionNombre,
                PrecioUnitarioCentavos = l.PrecioUnitarioCentavos
            }).ToList(),
            Pagos = venta.Pagos.Select(p => new PagoPayload
            {
                Medio = (int)p.Medio,
                MontoCentavos = p.MontoCentavos
            }).ToList()
        });

        Carrito.Clear();
        _ = _sync.SincronizarAhoraAsync();

        return new ResultadoCobro(venta, vuelto);
    }

    public CajaLocal AbrirCaja(int montoInicialCentavos)
    {
        var sesion = _sesiones.Actual ?? throw new DomainException("SESION_REQUERIDA", "No hay sesión iniciada.");
        if (CajaActiva is not null)
        {
            throw new DomainException("CAJA_YA_ABIERTA", "Ya hay una caja abierta.");
        }

        var caja = new CajaLocal
        {
            Id = Guid.NewGuid(),
            ComercioId = sesion.ComercioId,
            UsuarioId = sesion.UsuarioId,
            FechaApertura = DateTime.UtcNow,
            MontoInicialCentavos = montoInicialCentavos,
            Estado = EstadoCaja.ABIERTA
        };

        _almacen.AbrirCajaLocal(caja);
        _almacen.EncolarOperacion("ABRIR_CAJA", new AbrirCajaPayload
        {
            CajaId = caja.Id,
            MontoInicialCentavos = montoInicialCentavos,
            Fecha = caja.FechaApertura,
            UsuarioId = sesion.UsuarioId
        });
        _ = _sync.SincronizarAhoraAsync();

        return caja;
    }

    public IReadOnlyDictionary<MedioPago, int> CalcularEsperadoPorMedio()
    {
        var caja = CajaActiva ?? throw new DomainException("CAJA_NO_ABIERTA", "No hay una caja abierta.");
        var esperadoPorMedio = new Dictionary<MedioPago, int>();
        foreach (var medio in MediosDisponibles)
        {
            var baseEsperado = medio == MedioPago.EFECTIVO ? caja.MontoInicialCentavos : 0;
            esperadoPorMedio[medio] = baseEsperado + _almacen.SumarPagosDeCaja(caja.Id, medio);
        }

        return esperadoPorMedio;
    }

    public ResultadoCierreCaja CerrarCaja(IReadOnlyDictionary<MedioPago, int> declaradoPorMedio)
    {
        var caja = CajaActiva ?? throw new DomainException("CAJA_NO_ABIERTA", "No hay una caja abierta.");
        var esperadoPorMedio = CalcularEsperadoPorMedio();

        var totalEsperado = esperadoPorMedio.Values.Sum();
        var totalDeclarado = declaradoPorMedio.Values.Sum();
        var diferencia = totalDeclarado - totalEsperado;

        caja.Estado = EstadoCaja.CERRADA;
        caja.FechaCierre = DateTime.UtcNow;
        caja.MontoEsperadoCentavos = totalEsperado;
        caja.MontoDeclaradoCentavos = totalDeclarado;
        caja.DiferenciaCentavos = diferencia;

        _almacen.CerrarCajaLocal(caja);
        _almacen.EncolarOperacion("CERRAR_CAJA", new CerrarCajaPayload
        {
            CajaId = caja.Id,
            MontoEsperadoCentavos = totalEsperado,
            MontoDeclaradoCentavos = totalDeclarado,
            Fecha = caja.FechaCierre.Value
        });
        _ = _sync.SincronizarAhoraAsync();

        return new ResultadoCierreCaja(caja, esperadoPorMedio, totalEsperado, totalDeclarado, diferencia);
    }

    public string GenerarTicket(ResultadoCobro resultado)
    {
        var venta = resultado.Venta;
        var sesion = _sesiones.Actual;
        var sb = new StringBuilder();
        sb.AppendLine("            KIOSCO DEMO");
        sb.AppendLine("--------------------------------");
        sb.AppendLine($"Ticket N°:    {venta.Numero}");
        sb.AppendLine($"Fecha:        {venta.Fecha.ToLocalTime():dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Cajero:       {sesion?.Username ?? "-"}");
        sb.AppendLine("--------------------------------");
        foreach (var linea in venta.Lineas)
        {
            sb.AppendLine($"{linea.ProductoNombre} {linea.PresentacionNombre}");
            sb.AppendLine($"{linea.Cantidad} x {Pesos(linea.PrecioUnitarioCentavos)} = {Pesos(linea.SubtotalCentavos)}");
        }

        sb.AppendLine("--------------------------------");
        sb.AppendLine($"TOTAL:        {Pesos(venta.TotalCentavos)}");
        foreach (var pago in venta.Pagos)
        {
            sb.AppendLine($"{NombreMedio(pago.Medio)}: {Pesos(pago.MontoCentavos)}");
        }

        if (resultado.VueltoCentavos > 0)
        {
            sb.AppendLine($"VUELTO:       {Pesos(resultado.VueltoCentavos)}");
        }

        sb.AppendLine("--------------------------------");
        sb.AppendLine("   GRACIAS POR SU COMPRA");
        sb.AppendLine("   Ticket no fiscal (sin validez");
        sb.AppendLine("   impositiva segun RG 4095");
        return sb.ToString();
    }

    public static string NombreMedio(MedioPago medio) => medio switch
    {
        MedioPago.EFECTIVO => "Efectivo",
        MedioPago.TARJETA => "Tarjeta",
        MedioPago.TRANSFERENCIA_QR => "Transferencia/QR",
        _ => medio.ToString()
    };

    public static string Pesos(int centavos)
    {
        var pesos = centavos / 100m;
        return pesos.ToString("C", new System.Globalization.CultureInfo("es-AR"));
    }
}

public sealed record AbrirCajaPayload
{
    public Guid CajaId { get; init; }
    public int MontoInicialCentavos { get; init; }
    public DateTime Fecha { get; init; }
    public Guid UsuarioId { get; init; }
}

public sealed record CerrarCajaPayload
{
    public Guid CajaId { get; init; }
    public int MontoEsperadoCentavos { get; init; }
    public int MontoDeclaradoCentavos { get; init; }
    public DateTime Fecha { get; init; }
}

public sealed record LineaVentaPayload
{
    public Guid PresentacionId { get; init; }
    public int Cantidad { get; init; }
    public string ProductoNombre { get; init; } = string.Empty;
    public string PresentacionNombre { get; init; } = string.Empty;
    public int PrecioUnitarioCentavos { get; init; }
}

public sealed record PagoPayload
{
    public int Medio { get; init; }
    public int MontoCentavos { get; init; }
}

public sealed record VentaPayload
{
    public Guid VentaId { get; init; }
    public Guid CajaId { get; init; }
    public int Numero { get; init; }
    public DateTime Fecha { get; init; }
    public bool ClientGenerated { get; init; }
    public List<LineaVentaPayload> Lineas { get; init; } = [];
    public List<PagoPayload> Pagos { get; init; } = [];
}
