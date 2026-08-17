using Kiosk.Application.Puertos.Integraciones;

namespace Kiosk.Application.Tests.TestDoubles;

public sealed class FakeWhatsAppSender : IWhatsAppSender
{
    private readonly List<(string Numero, string Texto)> _enviados = [];

    public IReadOnlyList<(string Numero, string Texto)> Enviados => _enviados;

    public Task EnviarAsync(string whatsappNumero, string texto, CancellationToken cancellationToken = default)
    {
        _enviados.Add((whatsappNumero, texto));
        return Task.CompletedTask;
    }
}
