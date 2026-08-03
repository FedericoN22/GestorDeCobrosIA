using System.Globalization;

namespace Kiosk.Pos.Services;

public static class ConversorMontos
{
    public static int? PesosACentavos(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        var t = texto.Trim();
        var esAr = CultureInfo.GetCultureInfo("es-AR");
        if (decimal.TryParse(t, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, esAr, out var valor) ||
            decimal.TryParse(t, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.InvariantCulture, out valor))
        {
            return (int)Math.Round(valor * 100m);
        }

        return null;
    }

    public static string CentavosAPesos(int centavos)
    {
        var pesos = centavos / 100m;
        return pesos.ToString("N2", CultureInfo.GetCultureInfo("es-AR"));
    }
}
