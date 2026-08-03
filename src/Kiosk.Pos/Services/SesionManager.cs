using Kiosk.Pos.Models;

namespace Kiosk.Pos.Services;

public sealed class SesionManager
{
    private readonly AlmacenLocal _almacen;

    public SesionManager(AlmacenLocal almacen)
    {
        _almacen = almacen;
    }

    public Sesion? Actual { get; private set; }

    public event EventHandler? Cambio;

    public void Cargar()
    {
        Actual = _almacen.ObtenerSesion();
    }

    public void Iniciar(Sesion sesion)
    {
        Actual = sesion;
        _almacen.GuardarSesion(sesion);
        Cambio?.Invoke(this, EventArgs.Empty);
    }

    public void Cerrar()
    {
        Actual = null;
        _almacen.LimpiarSesion();
        Cambio?.Invoke(this, EventArgs.Empty);
    }
}
