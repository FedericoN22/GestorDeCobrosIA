namespace Kiosk.Domain.Configuracion;

public class Configuracion
{
    public Guid ComercioId { get; private set; }
    public string Clave { get; private set; } = null!;
    public string Valor { get; private set; } = null!;
    public DateTime UpdatedAt { get; private set; }

    private Configuracion() { }

    public static Configuracion Crear(Guid comercioId, string clave, string valor)
    {
        if (string.IsNullOrWhiteSpace(clave))
        {
            throw new Exception("La clave de configuración es obligatoria.");
        }

        return new Configuracion
        {
            ComercioId = comercioId,
            Clave = clave.Trim(),
            Valor = valor,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void CambiarValor(string valor)
    {
        Valor = valor;
        UpdatedAt = DateTime.UtcNow;
    }
}
