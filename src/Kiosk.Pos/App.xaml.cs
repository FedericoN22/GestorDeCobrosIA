using System.Windows;
using Kiosk.Pos.Services;

namespace Kiosk.Pos;

public partial class App : Application
{
    private AlmacenLocal _almacen = null!;
    private SesionManager _sesiones = null!;
    private ApiClient _api = null!;
    private SyncEngine _sync = null!;
    private GestorVentas _gestorVentas = null!;
    private MainWindow _ventana = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Directory.CreateDirectory(ConfiguracionPos.RutaBase);
        _almacen = new AlmacenLocal(Path.Combine(ConfiguracionPos.RutaBase, "pos.db"));
        _almacen.Inicializar();

        _sesiones = new SesionManager(_almacen);
        _sesiones.Cargar();

        _api = new ApiClient(ConfiguracionPos.ApiBaseUrl);
        _sync = new SyncEngine(_api, _almacen, _sesiones, ConfiguracionPos.IntervaloSync);
        var impresora = new ImpresoraArchivo();
        _gestorVentas = new GestorVentas(_almacen, _sesiones, _sync, impresora);

        _ventana = new MainWindow(_almacen, _sesiones, _api, _sync, _gestorVentas, impresora);
        _ventana.Show();

        if (_sesiones.Actual is not null)
        {
            _sync.Iniciar();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _sync.DetenerAsync().GetAwaiter().GetResult();
        base.OnExit(e);
    }
}
