using Kiosk.Application.Abstractions;
using Kiosk.Application.Auditoria;
using Kiosk.Application.Puertos;
using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Auditoria;
using Kiosk.Domain.Common;
using Kiosk.Domain.Configuracion;
using Kiosk.Domain.Whatsapp;

namespace Kiosk.Application.CasosUso.Whatsapp;

public sealed record WhitelistResult(Guid Id, string WhatsappNumero, bool Activo);

public sealed record AgregarWhitelistCommand(Guid ComercioId, string WhatsappNumero, string Actor, Canal Origen);

public sealed record QuitarWhitelistCommand(Guid ComercioId, Guid WhitelistId, string Actor, Canal Origen);

public sealed record ConfiguracionBotResult(
    string Nombre,
    string Bienvenida,
    int TiempoConfirmacionMinutos,
    int LimiteMensajesPorMinuto);

public sealed record GuardarConfiguracionBotCommand(
    Guid ComercioId,
    string Nombre,
    string Bienvenida,
    int TiempoConfirmacionMinutos,
    int LimiteMensajesPorMinuto,
    string Actor,
    Canal Origen);

public sealed class ServicioWhatsAppAdmin
{
    private readonly IWhatsAppWhitelistRepository _whitelist;
    private readonly IConfiguracionRepository _config;
    private readonly IAuditoriaRepository _auditoria;
    private readonly IUnitOfWork _unitOfWork;

    public ServicioWhatsAppAdmin(
        IWhatsAppWhitelistRepository whitelist,
        IConfiguracionRepository config,
        IAuditoriaRepository auditoria,
        IUnitOfWork unitOfWork)
    {
        _whitelist = whitelist;
        _config = config;
        _auditoria = auditoria;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<WhitelistResult>> ListarWhitelistAsync(Guid comercioId, CancellationToken cancellationToken = default)
    {
        var entradas = await _whitelist.ListarAsync(comercioId, cancellationToken);
        return entradas.Select(e => new WhitelistResult(e.Id, e.WhatsappNumero, e.Activo)).ToList();
    }

    public async Task<Result<WhitelistResult>> AgregarWhitelistAsync(AgregarWhitelistCommand command, CancellationToken cancellationToken = default)
    {
        var numero = command.WhatsappNumero?.Trim();
        if (string.IsNullOrWhiteSpace(numero))
        {
            return Result<WhitelistResult>.Fail(new Error("WHITELIST_NUMERO_REQUERIDO", "El número de WhatsApp es obligatorio."));
        }

        var existente = await _whitelist.GetAsync(command.ComercioId, numero, cancellationToken);
        if (existente is not null)
        {
            return Result<WhitelistResult>.Fail(
                new Error("WHITELIST_DUPLICADA", $"El número {numero} ya está en la whitelist."));
        }

        var entrada = WhatsappWhitelist.Autorizar(command.ComercioId, numero);
        _whitelist.Add(entrada);
        AuditoriaRegistrador.Registrar(_auditoria, command.ComercioId, command.Origen, command.Actor,
            AuditoriaTipos.WhitelistAgregada, new { entrada.Id, numero });
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<WhitelistResult>.Ok(new WhitelistResult(entrada.Id, entrada.WhatsappNumero, entrada.Activo));
    }

    public async Task<Result<WhitelistResult>> QuitarWhitelistAsync(QuitarWhitelistCommand command, CancellationToken cancellationToken = default)
    {
        var entrada = await _whitelist.GetByIdAsync(command.WhitelistId, cancellationToken);
        if (entrada is null || entrada.ComercioId != command.ComercioId)
        {
            return Result<WhitelistResult>.Fail(
                new Error("WHITELIST_NO_ENCONTRADA", "La entrada de la whitelist no existe o no pertenece al comercio."));
        }

        if (entrada.Activo)
        {
            entrada.Desactivar();
            AuditoriaRegistrador.Registrar(_auditoria, command.ComercioId, command.Origen, command.Actor,
                AuditoriaTipos.WhitelistQuitada, new { entrada.Id, entrada.WhatsappNumero });
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result<WhitelistResult>.Ok(new WhitelistResult(entrada.Id, entrada.WhatsappNumero, entrada.Activo));
    }

    public async Task<ConfiguracionBotResult> ObtenerConfiguracionBotAsync(Guid comercioId, CancellationToken cancellationToken = default)
    {
        var nombre = await ObtenerTextoAsync(comercioId, ClavesConfiguracion.BotNombre, cancellationToken) ?? "asistente";
        var bienvenida = await ObtenerTextoAsync(comercioId, ClavesConfiguracion.BotBienvenida, cancellationToken) ?? string.Empty;
        var minutos = await ObtenerEnteroAsync(comercioId, ClavesConfiguracion.BotConfirmacionMinutos, 2, cancellationToken);
        var limite = await ObtenerEnteroAsync(comercioId, ClavesConfiguracion.BotLimiteMensajesPorMinuto, 10, cancellationToken);

        return new ConfiguracionBotResult(nombre, bienvenida, minutos, limite);
    }

    public async Task<Result<ConfiguracionBotResult>> GuardarConfiguracionBotAsync(GuardarConfiguracionBotCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Nombre))
        {
            return Result<ConfiguracionBotResult>.Fail(new Error("CONFIG_NOMBRE_REQUERIDO", "El nombre del bot es obligatorio."));
        }

        if (command.TiempoConfirmacionMinutos is < 1 or > 60)
        {
            return Result<ConfiguracionBotResult>.Fail(
                new Error("CONFIG_CONFIRMACION_INVALIDA", "El tiempo de confirmación debe estar entre 1 y 60 minutos."));
        }

        if (command.LimiteMensajesPorMinuto is < 1 or > 100)
        {
            return Result<ConfiguracionBotResult>.Fail(
                new Error("CONFIG_LIMITE_INVALIDO", "El límite de mensajes debe estar entre 1 y 100 por minuto."));
        }

        await GuardarTextoAsync(command.ComercioId, ClavesConfiguracion.BotNombre, command.Nombre.Trim(), cancellationToken);
        await GuardarTextoAsync(command.ComercioId, ClavesConfiguracion.BotBienvenida, command.Bienvenida?.Trim() ?? string.Empty, cancellationToken);
        await GuardarTextoAsync(command.ComercioId, ClavesConfiguracion.BotConfirmacionMinutos, command.TiempoConfirmacionMinutos.ToString(), cancellationToken);
        await GuardarTextoAsync(command.ComercioId, ClavesConfiguracion.BotLimiteMensajesPorMinuto, command.LimiteMensajesPorMinuto.ToString(), cancellationToken);

        AuditoriaRegistrador.Registrar(_auditoria, command.ComercioId, command.Origen, command.Actor,
            AuditoriaTipos.ConfiguracionBotGuardada,
            new { command.Nombre, command.TiempoConfirmacionMinutos, command.LimiteMensajesPorMinuto });
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ConfiguracionBotResult>.Ok(new ConfiguracionBotResult(
            command.Nombre.Trim(),
            command.Bienvenida?.Trim() ?? string.Empty,
            command.TiempoConfirmacionMinutos,
            command.LimiteMensajesPorMinuto));
    }

    private async Task<string?> ObtenerTextoAsync(Guid comercioId, string clave, CancellationToken ct)
    {
        var config = await _config.GetAsync(comercioId, clave, ct);
        return config?.Valor;
    }

    private async Task<int> ObtenerEnteroAsync(Guid comercioId, string clave, int porDefecto, CancellationToken ct)
    {
        var valor = await ObtenerTextoAsync(comercioId, clave, ct);
        return int.TryParse(valor, out var n) ? n : porDefecto;
    }

    private async Task GuardarTextoAsync(Guid comercioId, string clave, string valor, CancellationToken ct)
    {
        var config = await _config.GetAsync(comercioId, clave, ct);
        if (config is null)
        {
            _config.Add(Configuracion.Crear(comercioId, clave, valor));
        }
        else
        {
            config.CambiarValor(valor);
        }
    }
}
