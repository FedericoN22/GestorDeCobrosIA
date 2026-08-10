namespace Kiosk.Application.CasosUso.Whatsapp;

public sealed class RateLimiterWhatsApp
{
    private readonly object _lock = new();
    private readonly Dictionary<string, Queue<DateTime>> _historial = new();

    public bool Permitir(string numero, int maxPorMinuto, DateTime ahora)
    {
        if (maxPorMinuto <= 0)
        {
            return false;
        }

        lock (_lock)
        {
            if (!_historial.TryGetValue(numero, out var ventana))
            {
                ventana = new Queue<DateTime>();
                _historial[numero] = ventana;
            }

            while (ventana.Count > 0 && ventana.Peek() <= ahora.AddMinutes(-1))
            {
                ventana.Dequeue();
            }

            if (ventana.Count >= maxPorMinuto)
            {
                return false;
            }

            ventana.Enqueue(ahora);
            return true;
        }
    }
}
