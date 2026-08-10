using System.Security.Cryptography;
using System.Text;

namespace Kiosk.Ia;

public static class FirmaVerificacion
{
    private const string Prefijo = "sha256=";

    public static bool EsValida(string? firmaHeader, string cuerpoCrudo, string? appSecret)
    {
        if (string.IsNullOrWhiteSpace(appSecret) || string.IsNullOrWhiteSpace(firmaHeader))
        {
            return false;
        }

        if (!firmaHeader.StartsWith(Prefijo, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var esperada = firmaHeader[Prefijo.Length..].Trim();
        var calculada = CalcularSha256(appSecret, cuerpoCrudo);
        return ComparacionSegura(esperada, calculada);
    }

    public static string CalcularSha256(string appSecret, string cuerpoCrudo)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
        var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(cuerpoCrudo));
        return Convert.ToHexStringLower(bytes);
    }

    private static bool ComparacionSegura(string a, string b)
    {
        if (a.Length != b.Length)
        {
            return false;
        }

        var diferencia = 0;
        for (var i = 0; i < a.Length; i++)
        {
            diferencia |= a[i] ^ b[i];
        }

        return diferencia == 0;
    }
}
