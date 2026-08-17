using Kiosk.Application.Puertos.Integraciones;

namespace Kiosk.Application.Tests.TestDoubles;

public sealed class FakeParser : IIaParser
{
    private readonly Queue<ResultadoParseo> _respuestas = new();
    private readonly List<string> _llamadas = [];

    public IReadOnlyList<string> Llamadas => _llamadas;

    public void Responder(ResultadoParseo resultado)
        => _respuestas.Enqueue(resultado);

    public Task<ResultadoParseo> ParsearAsync(string textoNormalizado, CancellationToken cancellationToken = default)
    {
        _llamadas.Add(textoNormalizado);
        return Task.FromResult(_respuestas.Count > 0
            ? _respuestas.Dequeue()
            : ResultadoParseo.Fallo("Sin respuesta configurada en el FakeParser."));
    }
}
