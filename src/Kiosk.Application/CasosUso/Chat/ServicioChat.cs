using Kiosk.Application.CasosUso.Chat;
using Kiosk.Domain.Common;

namespace Kiosk.Application.CasosUso.Chat;

public sealed class ServicioChat
{
    private readonly PipelineConversacional _pipeline;

    public ServicioChat(PipelineConversacional pipeline)
    {
        _pipeline = pipeline;
    }

    public Task<string> ProcesarAsync(
        Guid comercioId,
        Guid usuarioId,
        string texto,
        CancellationToken cancellationToken = default)
        => _pipeline.ProcesarAsync(comercioId, Canal.WEB, usuarioId.ToString(), texto, fueAudio: false, cancellationToken);
}
