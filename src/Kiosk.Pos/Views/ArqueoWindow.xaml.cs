using System.Windows;
using System.Windows.Controls;
using Kiosk.Domain.Ventas;
using Kiosk.Pos.Services;

namespace Kiosk.Pos.Views;

public partial class ArqueoWindow : Window
{
    public IReadOnlyDictionary<MedioPago, int> Declarado { get; private set; } = new Dictionary<MedioPago, int>();

    public ArqueoWindow(IReadOnlyDictionary<MedioPago, int> esperadoPorMedio, int totalEsperadoCentavos)
    {
        InitializeComponent();

        var efectivo = esperadoPorMedio.TryGetValue(MedioPago.EFECTIVO, out var e) ? e : 0;
        var tarjeta = esperadoPorMedio.TryGetValue(MedioPago.TARJETA, out var t) ? t : 0;
        var qr = esperadoPorMedio.TryGetValue(MedioPago.TRANSFERENCIA_QR, out var q) ? q : 0;

        LblDiferencia.Text = $"Monto esperado total: {GestorVentas.Pesos(totalEsperadoCentavos)}";
        TxtEfectivo.Text = ConversorMontos.CentavosAPesos(efectivo);
        TxtTarjeta.Text = ConversorMontos.CentavosAPesos(tarjeta);
        TxtQr.Text = ConversorMontos.CentavosAPesos(qr);
    }

    private void BtnConfirmar_Click(object sender, RoutedEventArgs e)
    {
        var efectivo = ConversorMontos.PesosACentavos(TxtEfectivo.Text);
        var tarjeta = ConversorMontos.PesosACentavos(TxtTarjeta.Text);
        var qr = ConversorMontos.PesosACentavos(TxtQr.Text);

        if (efectivo is null || tarjeta is null || qr is null)
        {
            LblError.Text = "Ingresá montos válidos en pesos (ej. 1234,50).";
            LblError.Visibility = Visibility.Visible;
            return;
        }

        Declarado = new Dictionary<MedioPago, int>
        {
            [MedioPago.EFECTIVO] = efectivo.Value,
            [MedioPago.TARJETA] = tarjeta.Value,
            [MedioPago.TRANSFERENCIA_QR] = qr.Value
        };
        DialogResult = true;
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
