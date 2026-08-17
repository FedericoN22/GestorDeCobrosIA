using System.Text.Json;
using Kiosk.Application.Auditoria;
using Kiosk.Application.Intenciones;
using Kiosk.Application.Puertos;
using Kiosk.Application.Puertos.Integraciones;
using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Auditoria;
using Kiosk.Domain.Common;
using Kiosk.Domain.Whatsapp;

namespace Kiosk.Application.CasosUso.Whatsapp;

public sealed class ServicioWhatsApp
{
    private static readonly HashSet<string> Confirmaciones = new(StringComparer.Ordinal)
    {
        "SI", "CONFIRMO", "OK", "DALE"
    };

    private static readonly HashSet<string> Cancelaciones = new(StringComparer.Ordinal)
    {
        "NO", "CANCELAR", "CANCELO"
    };

    private static readonly HashSet<string> Saludos = new(StringComparer.Ordinal)
    {
        "HOLA", "HOLAS", "BUENAS", "HI", "HII", "HELLO",
        "BUEN DIA", "BUENOS DIAS", "BUENAS TARDES", "BUENAS NOCHES"
    };

    private readonly IWhatsAppWhitelistRepository _whitelist;
    private readonly IIntencionRepository _intenciones;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguracionRepository _config;
    private readonly IAuditoriaRepository _auditoria;
    private readonly IIaParser _parser;
    private readonly IWhatsAppSender _sender;
    private readonly ResolvedorCatalogos _resolvedor;
    private readonly EjecutorAcciones _ejecutor;
    private readonly RateLimiterWhatsApp _rateLimiter;
    private readonly IStockLedger _stockLedger;

    public ServicioWhatsApp(
        IWhatsAppWhitelistRepository whitelist,
        IIntencionRepository intenciones,
        IUnitOfWork unitOfWork,
        IConfiguracionRepository config,
        IAuditoriaRepository auditoria,
        IIaParser parser,
        IWhatsAppSender sender,
        ResolvedorCatalogos resolvedor,
        EjecutorAcciones ejecutor,
        RateLimiterWhatsApp rateLimiter,
        IStockLedger stockLedger)
    {
        _whitelist = whitelist;
        _intenciones = intenciones;
        _unitOfWork = unitOfWork;
        _config = config;
        _auditoria = auditoria;
        _parser = parser;
        _sender = sender;
        _resolvedor = resolvedor;
        _ejecutor = ejecutor;
        _rateLimiter = rateLimiter;
        _stockLedger = stockLedger;
    }

    public async Task<string> ProcesarMensajeAsync(
        Guid comercioId,
        string whatsappNumero,
        string texto,
        bool fueAudio = false,
        CancellationToken cancellationToken = default)
    {
        Intencion? intencion = null;

        try
        {
            if (!await _whitelist.EstaAutorizadoAsync(comercioId, whatsappNumero, cancellationToken))
            {
                return await ResponderAsync(whatsappNumero, RespuestasBot.NoAutorizado, cancellationToken);
            }

            var limite = await ObtenerEnteroAsync(
                comercioId, ClavesConfiguracion.BotLimiteMensajesPorMinuto, 10, cancellationToken);

            if (!_rateLimiter.Permitir(whatsappNumero, limite, DateTime.UtcNow))
            {
                return await ResponderAsync(whatsappNumero, RespuestasBot.LimiteExcedido, cancellationToken);
            }

            var textoNormalizado = Normalizacion.Normalizar(texto);
            if (string.IsNullOrWhiteSpace(textoNormalizado))
            {
                var nombreVacio = await ObtenerNombreBotAsync(comercioId, cancellationToken);
                return await ResponderAsync(whatsappNumero, RespuestasBot.NoInterpretado(nombreVacio), cancellationToken);
            }

            if (EsSaludo(textoNormalizado))
            {
                var bienvenida = await ObtenerBienvenidaAsync(comercioId, cancellationToken);
                return await ResponderAsync(whatsappNumero, bienvenida, cancellationToken);
            }

            var pendiente = await _intenciones.GetPendienteAsync(comercioId, whatsappNumero, cancellationToken);
            if (pendiente is not null && pendiente.Estado == EstadoIntencion.ESPERANDO_CONFIRMACION)
            {
                if (pendiente.ConfirmacionExpirada(DateTime.UtcNow))
                {
                    pendiente.Cancelar("Timeout de confirmación expirado");
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    return await ResponderAsync(whatsappNumero, RespuestasBot.TimeoutExpirado, cancellationToken);
                }

                if (EsRespuestaConfirmacion(textoNormalizado))
                {
                    var respuesta = await ProcesarConfirmacionAsync(comercioId, pendiente, textoNormalizado, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    return await ResponderAsync(whatsappNumero, respuesta, cancellationToken);
                }

                pendiente.Cancelar("Reemplazada por una nueva intención");
            }
            else if (pendiente is not null)
            {
                pendiente.Cancelar("Reemplazada por una nueva intención");
            }

            intencion = Intencion.Recibir(comercioId, whatsappNumero, texto, fueAudio);
            _intenciones.Add(intencion);
            AuditoriaRegistrador.Registrar(_auditoria, comercioId, Canal.WHATSAPP, whatsappNumero,
                AuditoriaTipos.IntencionRecibida, new { intencion.Id, texto, fueAudio });
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var parseo = await _parser.ParsearAsync(textoNormalizado, cancellationToken);
            var respuestaFinal = await DecidirAsync(comercioId, intencion, parseo, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return await ResponderAsync(whatsappNumero, respuestaFinal, cancellationToken);
        }
        catch (Exception ex)
        {
            if (intencion is not null
                && intencion.Estado is not (EstadoIntencion.EJECUTADA or EstadoIntencion.CANCELADA or EstadoIntencion.RECHAZADA or EstadoIntencion.ERROR))
            {
                intencion.MarcarError(ex.Message);
                try
                {
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
                catch
                {
                    // el error de persistencia no debe romper la respuesta al usuario
                }
            }

            return await ResponderAsync(whatsappNumero, RespuestasBot.Error, cancellationToken);
        }
    }

    private async Task<string> DecidirAsync(Guid comercioId, Intencion intencion, ResultadoParseo parseo, CancellationToken ct)
    {
        var numero = intencion.WhatsappNumero;

        if (parseo.EsMultiComando)
        {
            intencion.Rechazar(parseo.Motivo ?? "Más de un comando en un solo mensaje");
            AuditoriaRegistrador.Registrar(_auditoria, comercioId, Canal.WHATSAPP, numero,
                AuditoriaTipos.IntencionRechazada, new { intencion.Id, motivo = parseo.Motivo });
            return RespuestasBot.MultiComando;
        }

        if (parseo.EsFallo)
        {
            var nombreBot = await ObtenerNombreBotAsync(comercioId, ct);
            intencion.Rechazar(parseo.Motivo ?? "Mensaje no interpretable");
            AuditoriaRegistrador.Registrar(_auditoria, comercioId, Canal.WHATSAPP, numero,
                AuditoriaTipos.IntencionRechazada, new { intencion.Id, motivo = parseo.Motivo });
            return RespuestasBot.NoInterpretado(nombreBot);
        }

        var comando = parseo.Comando!;
        intencion.MarcarParseada(JsonSerializer.Serialize(comando));

        if (!comando.TieneConfianzaSuficiente)
        {
            var nombreBot = await ObtenerNombreBotAsync(comercioId, ct);
            intencion.PedirAclaracion($"Confianza {comando.Confianza:P0} inferior al umbral");
            AuditoriaRegistrador.Registrar(_auditoria, comercioId, Canal.WHATSAPP, numero,
                AuditoriaTipos.IntencionAclaracion, new { intencion.Id, confianza = comando.Confianza });
            return RespuestasBot.ConfianzaBaja(nombreBot);
        }

        var faltantes = new List<string>();
        foreach (var f in comando.CamposFaltantes)
        {
            if (!string.IsNullOrWhiteSpace(f) && !faltantes.Contains(f))
            {
                faltantes.Add(f);
            }
        }

        foreach (var f in ValidadorComando.CalcularFaltantes(comando))
        {
            if (!faltantes.Contains(f))
            {
                faltantes.Add(f);
            }
        }

        var ambiguos = comando.CamposAmbiguos.Where(a => !string.IsNullOrWhiteSpace(a)).ToList();

        if (faltantes.Count > 0 || ambiguos.Count > 0)
        {
            intencion.PedirAclaracion(JsonSerializer.Serialize(new { faltantes, ambiguos }));
            AuditoriaRegistrador.Registrar(_auditoria, comercioId, Canal.WHATSAPP, numero,
                AuditoriaTipos.IntencionAclaracion, new { intencion.Id, faltantes, ambiguos });
            return RespuestasBot.FaltanCampos(faltantes, ambiguos);
        }

        var resolucion = await _resolvedor.ResolverAsync(comercioId, comando, ct);

        if (resolucion.Estado == EstadoResolucion.NO_ENCONTRADO)
        {
            intencion.Rechazar(resolucion.Motivo ?? "No se encontró el objetivo en el catálogo.");
            AuditoriaRegistrador.Registrar(_auditoria, comercioId, Canal.WHATSAPP, numero,
                AuditoriaTipos.IntencionRechazada, new { intencion.Id, motivo = resolucion.Motivo });
            return RespuestasBot.NoEncontrado(resolucion.Motivo ?? "No se encontró el objetivo en el catálogo.");
        }

        if (resolucion.Estado == EstadoResolucion.AMBIGUO)
        {
            var presentaciones = resolucion.Candidatos
                .Select(c => c.Presentacion.Nombre)
                .Distinct()
                .ToList();

            intencion.PedirAclaracion("Presentación ambigua");
            AuditoriaRegistrador.Registrar(_auditoria, comercioId, Canal.WHATSAPP, numero,
                AuditoriaTipos.IntencionAclaracion, new { intencion.Id, presentaciones });
            return RespuestasBot.ElegiPresentacion(comando.Parametros.Producto ?? "", presentaciones);
        }

        if (comando.EsDestructivo)
        {
            var minutos = await ObtenerEnteroAsync(comercioId, ClavesConfiguracion.BotConfirmacionMinutos, 2, ct);
            var expira = DateTime.UtcNow.AddMinutes(Math.Max(1, minutos));
            intencion.PedirConfirmacion(expira);

            var mensajeConfirmacion = await ConfirmacionDestructivaAsync(comercioId, comando, resolucion.Coincidencia, ct);
            AuditoriaRegistrador.Registrar(_auditoria, comercioId, Canal.WHATSAPP, numero,
                AuditoriaTipos.IntencionConfirmacion, new { intencion.Id, expiraEn = expira, comando.Accion });
            return mensajeConfirmacion;
        }

        return await EjecutarYResponderAsync(comercioId, intencion, comando, resolucion.Coincidencia, ct);
    }

    private async Task<string> ConfirmacionDestructivaAsync(
        Guid comercioId,
        StructuredCommand comando,
        CoincidenciaPresentacion? objetivo,
        CancellationToken ct)
    {
        if (objetivo is null)
        {
            return "¿Confirmás la operación? Respondé SI o NO.";
        }

        if (comando.Accion == AccionIntencion.MODIFICAR_PRECIO)
        {
            return RespuestasBot.ConfirmarModificarPrecio(
                objetivo.Producto.Nombre,
                objetivo.Presentacion.Nombre,
                objetivo.Presentacion.PrecioVentaCentavos,
                comando.Parametros.Precio!.Value * 100);
        }

        if (comando.Accion == AccionIntencion.ELIMINAR_PRODUCTO)
        {
            var stock = await _stockLedger.CalcularStockAsync(objetivo.Presentacion.Id, ct);
            return RespuestasBot.ConfirmarEliminar(
                objetivo.Producto.Nombre,
                objetivo.Presentacion.Nombre,
                stock);
        }

        return "¿Confirmás la operación? Respondé SI o NO.";
    }

    private async Task<string> EjecutarYResponderAsync(
        Guid comercioId,
        Intencion intencion,
        StructuredCommand comando,
        CoincidenciaPresentacion? objetivo,
        CancellationToken ct)
    {
        var resultado = await _ejecutor.EjecutarAsync(comercioId, comando, objetivo, intencion.WhatsappNumero, ct);

        if (!resultado)
        {
            intencion.MarcarError(resultado.Error?.Message ?? "Error de ejecución");
            AuditoriaRegistrador.Registrar(_auditoria, comercioId, Canal.WHATSAPP, intencion.WhatsappNumero,
                AuditoriaTipos.IntencionError, new { intencion.Id, motivo = resultado.Error?.Message });
            return RespuestasBot.ErrorConMotivo(resultado.Error?.Message ?? "error desconocido");
        }

        intencion.Ejecutar(JsonSerializer.Serialize(new { respuesta = resultado.Value }));
        AuditoriaRegistrador.Registrar(_auditoria, comercioId, Canal.WHATSAPP, intencion.WhatsappNumero,
            AuditoriaTipos.IntencionEjecutada, new { intencion.Id, comando.Accion });
        return resultado.Value!;
    }

    private async Task<string> ProcesarConfirmacionAsync(
        Guid comercioId,
        Intencion pendiente,
        string textoNormalizado,
        CancellationToken ct)
    {
        if (Confirmaciones.Contains(textoNormalizado))
        {
            var comando = DeserializarComando(pendiente.StructuredCommandJson);
            var resolucion = await _resolvedor.ResolverAsync(comercioId, comando, ct);

            if (resolucion.Estado is not (EstadoResolucion.OK or EstadoResolucion.NO_APLICA))
            {
                pendiente.Cancelar($"No se pudo ejecutar: {resolucion.Motivo}");
                AuditoriaRegistrador.Registrar(_auditoria, comercioId, Canal.WHATSAPP, pendiente.WhatsappNumero,
                    AuditoriaTipos.IntencionCancelada, new { pendiente.Id, motivo = resolucion.Motivo });
                return RespuestasBot.CanceladoPorError(resolucion.Motivo ?? "el objetivo ya no existe");
            }

            var resultado = await _ejecutor.EjecutarAsync(comercioId, comando, resolucion.Coincidencia, pendiente.WhatsappNumero, ct);

            if (!resultado)
            {
                pendiente.MarcarError(resultado.Error?.Message ?? "Error de ejecución");
                AuditoriaRegistrador.Registrar(_auditoria, comercioId, Canal.WHATSAPP, pendiente.WhatsappNumero,
                    AuditoriaTipos.IntencionError, new { pendiente.Id, motivo = resultado.Error?.Message });
                return RespuestasBot.ErrorConMotivo(resultado.Error?.Message ?? "error desconocido");
            }

            pendiente.Ejecutar(JsonSerializer.Serialize(new { respuesta = resultado.Value }));
            AuditoriaRegistrador.Registrar(_auditoria, comercioId, Canal.WHATSAPP, pendiente.WhatsappNumero,
                AuditoriaTipos.IntencionConfirmacion, new { pendiente.Id, confirmado = true });
            AuditoriaRegistrador.Registrar(_auditoria, comercioId, Canal.WHATSAPP, pendiente.WhatsappNumero,
                AuditoriaTipos.IntencionEjecutada, new { pendiente.Id, comando.Accion });
            return resultado.Value!;
        }

        pendiente.Cancelar("Usuario canceló la confirmación");
        AuditoriaRegistrador.Registrar(_auditoria, comercioId, Canal.WHATSAPP, pendiente.WhatsappNumero,
            AuditoriaTipos.IntencionCancelada, new { pendiente.Id, confirmado = false });
        return RespuestasBot.OperacionCancelada;
    }

    private async Task<string> ResponderAsync(string whatsappNumero, string texto, CancellationToken ct)
    {
        try
        {
            await _sender.EnviarAsync(whatsappNumero, texto, ct);
        }
        catch
        {
            // un fallo de envío no debe romper el pipeline
        }

        return texto;
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

    private async Task<string> ObtenerNombreBotAsync(Guid comercioId, CancellationToken ct)
        => await ObtenerTextoAsync(comercioId, ClavesConfiguracion.BotNombre, ct) ?? "asistente";

    private async Task<string> ObtenerBienvenidaAsync(Guid comercioId, CancellationToken ct)
    {
        var nombre = await ObtenerNombreBotAsync(comercioId, ct);
        return await ObtenerTextoAsync(comercioId, ClavesConfiguracion.BotBienvenida, ct)
            ?? RespuestasBot.Ayuda(nombre);
    }

    private static bool EsSaludo(string normalizado)
    {
        var limpio = normalizado.TrimEnd('!', '¡', '?', '¿', '.', ' ').Trim();
        return Saludos.Contains(limpio)
            || limpio.StartsWith("AYUDA", StringComparison.Ordinal)
            || limpio == "COMANDOS";
    }

    private static bool EsRespuestaConfirmacion(string normalizado)
        => Confirmaciones.Contains(normalizado) || Cancelaciones.Contains(normalizado);

    private static StructuredCommand DeserializarComando(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("La intención no tiene un comando estructurado.");
        }

        return JsonSerializer.Deserialize<StructuredCommand>(json) ?? throw new InvalidOperationException("Comando estructurado inválido.");
    }
}
