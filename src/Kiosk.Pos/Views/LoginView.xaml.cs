using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Kiosk.Pos.Models;
using Kiosk.Pos.Services;

namespace Kiosk.Pos.Views;

public partial class LoginView : UserControl
{
    private ApiClient? _api;
    private SesionManager? _sesiones;
    private bool _cargando;

    public LoginView()
    {
        InitializeComponent();
    }

    public void Configurar(ApiClient api, SesionManager sesiones)
    {
        _api = api;
        _sesiones = sesiones;
    }

    private async void BtnIngresar_Click(object sender, RoutedEventArgs e)
    {
        await IntentarLoginAsync();
    }

    private void BtnIngresarOffline_Click(object sender, RoutedEventArgs e)
    {
        TxtError.Visibility = Visibility.Collapsed;
        var sesion = _sesiones?.Actual;
        if (sesion is null)
        {
            MostrarError("No hay una sesión guardada en esta PC.");
            return;
        }

        _sesiones!.Iniciar(sesion);
    }

    private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _ = IntentarLoginAsync();
        }
    }

    private async Task IntentarLoginAsync()
    {
        if (_cargando || _api is null || _sesiones is null)
        {
            return;
        }

        var username = TxtUsuario.Text.Trim();
        var password = TxtPassword.Password;

        if (username.Length == 0 || password.Length == 0)
        {
            MostrarError("Ingresá usuario y contraseña.");
            return;
        }

        _cargando = true;
        BtnIngresar.IsEnabled = false;
        TxtError.Visibility = Visibility.Collapsed;

        var resultado = await _api.LoginAsync(username, password);

        _cargando = false;
        BtnIngresar.IsEnabled = true;

        if (!resultado.Ok)
        {
            MostrarError(resultado.Error == "CONEXION"
                ? "Sin conexión con el servidor. Si ya iniciaste sesión antes en esta PC, podés continuar sin conexión."
                : resultado.Mensaje ?? "No se pudo iniciar sesión.");

            BtnIngresarOffline.Visibility = _sesiones.Actual is not null
                ? Visibility.Visible
                : Visibility.Collapsed;
            return;
        }

        var usuario = resultado.Valor!.Usuario;
        _sesiones.Iniciar(new Sesion
        {
            Token = resultado.Valor.Token,
            ComercioId = usuario.ComercioId,
            UsuarioId = usuario.Id,
            Username = usuario.Username,
            Nombre = usuario.Nombre,
            Rol = usuario.Rol
        });

        TxtPassword.Clear();
    }

    private void MostrarError(string mensaje)
    {
        TxtError.Text = mensaje;
        TxtError.Visibility = Visibility.Visible;
    }
}
