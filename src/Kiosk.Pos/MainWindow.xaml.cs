using System.Windows;
using Kiosk.Pos.Services;

namespace Kiosk.Pos;

public partial class MainWindow : Window
{
    private readonly SesionManager _sesiones;
    private readonly SyncEngine _sync;

    public MainWindow(AlmacenLocal almacen, SesionManager sesiones, ApiClient api, SyncEngine sync, GestorVentas gestorVentas, IPosPrinter impresora)
    {
        InitializeComponent();
        _sesiones = sesiones;
        _sync = sync;

        Login.Configurar(api, sesiones);
        Pos.Configurar(almacen, sesiones, api, sync, gestorVentas, impresora);

        _sesiones.Cambio += Sesiones_Cambio;
        MostrarVista();
    }

    private void Sesiones_Cambio(object? sender, EventArgs e)
    {
        Dispatcher.InvokeAsync(MostrarVista);
    }

    private void MostrarVista()
    {
        var tieneSesion = _sesiones.Actual is not null;
        Login.Visibility = tieneSesion ? Visibility.Collapsed : Visibility.Visible;
        Pos.Visibility = tieneSesion ? Visibility.Visible : Visibility.Collapsed;

        if (tieneSesion)
        {
            Pos.AlIniciar();
            _sync.Iniciar();
        }
        else
        {
            _sync.DetenerAsync().GetAwaiter().GetResult();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _sesiones.Cambio -= Sesiones_Cambio;
        base.OnClosed(e);
    }
}
