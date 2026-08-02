using System.Globalization;
using System.Text;

namespace Kiosk.Domain.Common;

public static class Normalizacion
{
    public static string Normalizar(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return string.Empty;
        }

        var sinDiacriticos = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(sinDiacriticos.Length);

        foreach (var c in sinDiacriticos)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb.ToString()
            .Normalize(NormalizationForm.FormC)
            .ToUpperInvariant()
            .Trim();
    }
}
