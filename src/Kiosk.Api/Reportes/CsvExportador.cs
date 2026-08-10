using System.Globalization;
using System.Text;

namespace Kiosk.Api.Reportes;

internal static class CsvExportador
{
    private static readonly CultureInfo EsAr = CultureInfo.GetCultureInfo("es-AR");

    public static string Monto(int centavos) => (centavos / 100m).ToString("N2", EsAr);

    public static string Fecha(DateTime fecha) => fecha.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

    public static string FechaDia(DateTime fecha) => fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static string Fila(params string?[] campos) => string.Join(";", campos.Select(Campo));

    public static byte[] Bytes(string contenido)
    {
        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        return utf8.GetPreamble().Concat(utf8.GetBytes(contenido)).ToArray();
    }

    private static string Campo(string? valor)
    {
        if (valor is null)
        {
            return string.Empty;
        }

        if (valor.Contains(';') || valor.Contains('"') || valor.Contains('\n') || valor.Contains('\r'))
        {
            return "\"" + valor.Replace("\"", "\"\"") + "\"";
        }

        return valor;
    }
}
