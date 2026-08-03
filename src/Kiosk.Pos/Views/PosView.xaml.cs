using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Kiosk.Domain.Common;
using Kiosk.Pos.Models;
using Kiosk.Pos.Services;

namespace Kiosk.Pos.Views;

public partial class PosView : UserControl
{
    private AlmacenLocal _almacen = null!;
    private SesionManager _sesiones = null!;
    private ApiClient _api = null!;
    private SyncEngine _sync = null!;
    private GestorVentas _gestor = null!;
    private IPosPrinter _impresora = null!;
    private bool _suscrito;

    public PosView()
    {
        InitializeComponent();
    }

    public void Configurar(AlmacenLocal almacen, SesionManager sesiones, ApiClient api, SyncEngine sync, GestorVentas gestor, IPosPrinter impresora)
    {
        _almacen = almacen;
        _sesiones = sesiones;
        _api = api;
        _sync = sync;
        _gestor = gestor;
        _impresora = impresora;
    }

    public void AlIniciar()
    {
        if (!_suscrito)
        {
            _sync.EstadoActualizado += Sync_EstadoActualizado;
            _sync.OperacionConError += Sync_OperacionConError;
            _sync.OperacionRechazada += Sync_OperacionRechazada;
            _suscrito = true;
        }

        LblUsuario.Text = _sesiones.Actual is { } s ? $"{s.Nombre} ({s.Rol})" : "";
        LimpiarCarrito();
        ActualizarCaja();
        AplicarEstadoSync(new EstadoSync { Online = _api.Online, Pendientes = 0, Errores = 0, UltimaSincronizacion = null });
    }

    // ================= Búsqueda =================

    private void BtnBuscar_Click(object sender, RoutedEventArgs e) => Buscar();

    private void TxtBusqueda_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Buscar();
        }
    }

    private void Buscar()
    {
        ListResultados.ItemsSource = null;
        ListResultados.ItemsSource = _almacen.BuscarProductos(TxtBusqueda.Text).ToList();
    }

    // ================= Carrito =================

    private void BtnAgregar_Click(object sender, RoutedEventArgs e) => AgregarSeleccionado();

    private void ListResultados_MouseDoubleClick(object sender, MouseButtonEventArgs e) => AgregarSeleccionado();

    private void AgregarSeleccionado()
    {
        if (ListResultados.SelectedItem is not ResultadoBusqueda producto)
        {
            MostrarMensaje("Seleccioná un producto de los resultados.");
            return;
        }

        try
        {
            _gestor.AgregarAlCarrito(producto, LeerCantidad());
            ActualizarCarrito();
            TxtMensaje.Visibility = Visibility.Collapsed;
        }
        catch (DomainException ex)
        {
            MostrarMensaje(ex.Message);
        }
    }

    private int LeerCantidad()
    {
        var texto = TxtCantidad.Text.Trim();
        return int.TryParse(texto, out var c) && c > 0 ? c : 1;
    }

    private void BtnSumar_Click(object sender, RoutedEventArgs e) => CambiarCantidadLinea(1);

    private void BtnRestar_Click(object sender, RoutedEventArgs e) => CambiarCantidadLinea(-1);

    private void CambiarCantidadLinea(int delta)
    {
        var indice = GridCarrito.SelectedIndex;
        if (indice < 0 || indice >= _gestor.Carrito.Count)
        {
            MostrarMensaje("Seleccioná una línea del carrito.");
            return;
        }

        try
        {
            _gestor.CambiarCantidad(indice, _gestor.Carrito[indice].Cantidad + delta);
            ActualizarCarrito();
            TxtMensaje.Visibility = Visibility.Collapsed;
        }
        catch (DomainException ex)
        {
            MostrarMensaje(ex.Message);
        }
    }

    private void BtnQuitar_Click(object sender, RoutedEventArgs e)
    {
        var indice = GridCarrito.SelectedIndex;
        if (indice >= 0 && indice < _gestor.Carrito.Count)
        {
            _gestor.QuitarDelCarrito(indice);
            ActualizarCarrito();
        }
    }

    private void BtnVaciar_Click(object sender, RoutedEventArgs e)
    {
        LimpiarCarrito();
    }

    private void LimpiarCarrito()
    {
        _gestor.Carrito.Clear();
        ActualizarCarrito();
    }

    private void ActualizarCarrito()
    {
        GridCarrito.ItemsSource = _gestor.Carrito.ToList();
        LblTotal.Text = GestorVentas.Pesos(_gestor.TotalCentavos);
        BtnCobrar.IsEnabled = _gestor.CajaActiva is not null && _gestor.Carrito.Count > 0;
        ActualizarVuelto();
    }

    // ================= Cobro =================

    private void TxtEfectivo_TextChanged(object sender, TextChangedEventArgs e) => ActualizarVuelto();

    private void ActualizarVuelto()
    {
        var total = _gestor.TotalCentavos;
        var tarjeta = ConversorMontos.PesosACentavos(TxtTarjeta.Text) ?? 0;
        var qr = ConversorMontos.PesosACentavos(TxtQr.Text) ?? 0;
        var otros = tarjeta + qr;
        var efectivo = ConversorMontos.PesosACentavos(TxtEfectivo.Text) ?? 0;

        if (otros > total)
        {
            LblVuelto.Text = "Excede total";
            LblVuelto.Foreground = new SolidColorBrush(Color.FromRgb(0xB4, 0x23, 0x18));
            return;
        }

        var vuelto = Math.Max(0, efectivo - (total - otros));
        LblVuelto.Text = vuelto > 0 ? GestorVentas.Pesos(vuelto) : "$0,00";
        LblVuelto.Foreground = new SolidColorBrush(Color.FromRgb(0x22, 0xA0, 0x6B));
    }

    private void BtnExacto_Click(object sender, RoutedEventArgs e)
    {
        TxtEfectivo.Text = ConversorMontos.CentavosAPesos(_gestor.TotalCentavos);
    }

    private void BtnMontoRapido_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is string pesos && int.TryParse(pesos, out var p))
        {
            var actual = ConversorMontos.PesosACentavos(TxtEfectivo.Text) ?? 0;
            TxtEfectivo.Text = ConversorMontos.CentavosAPesos(actual + p * 100);
        }
    }

    private void BtnCobrar_Click(object sender, RoutedEventArgs e)
    {
        var efectivo = ConversorMontos.PesosACentavos(TxtEfectivo.Text) ?? 0;
        var tarjeta = ConversorMontos.PesosACentavos(TxtTarjeta.Text) ?? 0;
        var qr = ConversorMontos.PesosACentavos(TxtQr.Text) ?? 0;

        try
        {
            var resultado = _gestor.Cobrar(new CobroInfo(efectivo, tarjeta, qr));
            var ticket = _gestor.GenerarTicket(resultado);
            _impresora.ImprimirTicket(ticket);

            MostrarMensaje(
                $"Venta N° {resultado.Venta.Numero} registrada. Vuelto: {GestorVentas.Pesos(resultado.VueltoCentavos)}",
                esError: false);

            TxtEfectivo.Clear();
            TxtTarjeta.Clear();
            TxtQr.Clear();
            ActualizarCarrito();
        }
        catch (DomainException ex)
        {
            MostrarMensaje(ex.Message);
        }
    }

    // ================= Caja =================

    private void BtnAbrirCaja_Click(object sender, RoutedEventArgs e)
    {
        var monto = ConversorMontos.PesosACentavos(TxtMontoInicial.Text) ?? 0;
        if (monto < 0)
        {
            MostrarMensaje("El fondo inicial no puede ser negativo.");
            return;
        }

        try
        {
            _gestor.AbrirCaja(monto);
            ActualizarCaja();
            MostrarMensaje($"Caja abierta con {GestorVentas.Pesos(monto)} de fondo.", esError: false);
        }
        catch (DomainException ex)
        {
            MostrarMensaje(ex.Message);
        }
    }

    private void BtnCerrarCaja_Click(object sender, RoutedEventArgs e)
    {
        var pendientes = _almacen.ContarPendientes();
        var errores = _almacen.ContarConErrores();
        if (pendientes > 0 || errores > 0)
        {
            var aviso = pendientes > 0 ? $"{pendientes} operación(es) pendiente(s) de sincronizar." : "";
            if (errores > 0)
            {
                aviso += $" {(aviso.Length > 0 ? "Además, " : "")}{errores} con error.";
            }

            if (MessageBox.Show($"Atención: {aviso}\n¿Cerrar la caja igualmente?",
                    "Cierre de caja", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }
        }

        try
        {
            var esperadoPorMedio = _gestor.CalcularEsperadoPorMedio();
            var ventana = new ArqueoWindow(esperadoPorMedio, esperadoPorMedio.Values.Sum())
            {
                Owner = Window.GetWindow(this)
            };
            if (ventana.ShowDialog() != true)
            {
                return;
            }

            var resultado = _gestor.CerrarCaja(ventana.Declarado);
            var signo = resultado.DiferenciaCentavos switch
            {
                > 0 => " (sobra)",
                < 0 => " (falta)",
                _ => ""
            };
            MostrarMensaje(
                $"Caja cerrada. Esperado: {GestorVentas.Pesos(resultado.TotalEsperadoCentavos)} · " +
                $"Declarado: {GestorVentas.Pesos(resultado.TotalDeclaradoCentavos)} · " +
                $"Diferencia: {GestorVentas.Pesos(resultado.DiferenciaCentavos)}{signo}",
                esError: false);
            ActualizarCaja();
        }
        catch (DomainException ex)
        {
            MostrarMensaje(ex.Message);
        }
    }

    private void ActualizarCaja()
    {
        var caja = _gestor.CajaActiva;
        var abierta = caja is not null;

        LblCaja.Text = abierta ? "Abierta" : "Cerrada";
        LblCajaDetalle.Text = abierta
            ? $"{GestorVentas.Pesos(caja!.MontoInicialCentavos)} de fondo · desde {caja.FechaApertura.ToLocalTime():dd/MM HH:mm}"
            : "";
        LblCajaAbierta.Text = "";
        BtnAbrirCaja.Visibility = abierta ? Visibility.Collapsed : Visibility.Visible;
        BtnCerrarCaja.Visibility = abierta ? Visibility.Visible : Visibility.Collapsed;
        TxtMontoInicial.IsEnabled = !abierta;
        BtnCobrar.IsEnabled = abierta && _gestor.Carrito.Count > 0;
    }

    // ================= Sync =================

    private void Sync_EstadoActualizado(object? sender, EstadoSync e)
        => Dispatcher.InvokeAsync(() => AplicarEstadoSync(e));

    private void Sync_OperacionConError(object? sender, string mensaje)
        => Dispatcher.InvokeAsync(() => MostrarMensaje(mensaje, esError: true));

    private void Sync_OperacionRechazada(object? sender, ResultadoOperacionDto res)
    {
        Dispatcher.InvokeAsync(() =>
        {
            if (res.Error == "CAJA_YA_ABIERTA" && _gestor.CajaActiva is { } caja)
            {
                _almacen.ForzarCierreCajaLocal(caja.Id);
                ActualizarCaja();
                MostrarMensaje(
                    "El servidor ya tiene una caja abierta: se cerró la caja local. " +
                    "Cerrá la caja del otro dispositivo para poder abrir una nueva.",
                    esError: true);
            }
        });
    }

    private void AplicarEstadoSync(EstadoSync e)
    {
        DotConexion.Fill = new SolidColorBrush(
            e.Online ? Color.FromRgb(0x22, 0xC5, 0x5E) : Color.FromRgb(0xDC, 0x26, 0x26));
        LblConexion.Text = e.Online ? "Online" : "Offline";
        LblPendientes.Text = $"Cola: {e.Pendientes}";
        LblUltimaSync.Text = e.UltimaSincronizacion is null
            ? "Última sync: -"
            : $"Última sync: {e.UltimaSincronizacion.Value.ToLocalTime():HH:mm:ss}";
    }

    // ================= Sesión =================

    private void BtnSalir_Click(object sender, RoutedEventArgs e) => CerrarSesion();

    private void BtnCambiarUsuario_Click(object sender, RoutedEventArgs e) => CerrarSesion();

    private void CerrarSesion()
    {
        var pendientes = _almacen.ContarPendientes();
        if (pendientes > 0 &&
            MessageBox.Show($"Hay {pendientes} operación(es) sin sincronizar. ¿Salir igualmente?",
                "Salir de sesión", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        _sesiones.Cerrar();
    }

    // ================= Utilidades =================

    private void MostrarMensaje(string mensaje, bool esError = true)
    {
        TxtMensaje.Text = mensaje;
        TxtMensaje.Foreground = new SolidColorBrush(
            esError ? Color.FromRgb(0xB4, 0x23, 0x18) : Color.FromRgb(0x06, 0x70, 0x47));
        TxtMensaje.Visibility = Visibility.Visible;
    }
}
