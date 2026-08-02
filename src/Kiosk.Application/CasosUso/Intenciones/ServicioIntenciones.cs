using Kiosk.Application.Abstractions;
using Kiosk.Application.Puertos;
using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Whatsapp;

namespace Kiosk.Application.CasosUso.Intenciones;

public sealed record RecibirIntencionCommand(Guid ComercioId, string WhatsappNumero, string Texto, bool FueAudio = false);

public sealed record ProcesarRespuestaCommand(Guid ComercioId, string WhatsappNumero, string Texto);

public sealed class ServicioIntenciones
{
    private readonly IWhatsAppWhitelistRepository _whitelist;
    private readonly IIntencionRepository _intenciones;
    private readonly IUnitOfWork _unitOfWork;

    public ServicioIntenciones(
        IWhatsAppWhitelistRepository whitelist,
        IIntencionRepository intenciones,
        IUnitOfWork unitOfWork)
    {
        _whitelist = whitelist;
        _intenciones = intenciones;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> RecibirAsync(RecibirIntencionCommand command, CancellationToken cancellationToken = default)
    {
        if (!await _whitelist.EstaAutorizadoAsync(command.ComercioId, command.WhatsappNumero, cancellationToken))
        {
            return Result<Guid>.Fail(new Error("WA_NO_AUTORIZADO", "El número no está autorizado para operar por WhatsApp."));
        }

        var intencion = Intencion.Recibir(command.ComercioId, command.WhatsappNumero, command.Texto, command.FueAudio);
        _intenciones.Add(intencion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Ok(intencion.Id);
    }

    public async Task<Result> ProcesarRespuestaAsync(ProcesarRespuestaCommand command, CancellationToken cancellationToken = default)
    {
        var pendiente = await _intenciones.GetPendienteAsync(command.WhatsappNumero, cancellationToken);
        if (pendiente is null || pendiente.Estado != EstadoIntencion.ESPERANDO_CONFIRMACION)
        {
            return Result.Fail(new Error("WA_SIN_CONFIRMACION_PENDIENTE", "No hay una confirmación pendiente para este número."));
        }

        var texto = command.Texto.Trim().ToUpperInvariant();
        if (texto is "SI" or "CONFIRMO" or "OK" or "DALE")
        {
            pendiente.Ejecutar("{\"confirmado\":true}");
        }
        else if (texto is "NO" or "CANCELAR" or "CANCELO")
        {
            pendiente.Cancelar();
        }
        else
        {
            return Result.Fail(new Error("WA_RESPUESTA_INVALIDA", "Respondé SI o CANCELAR."));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
