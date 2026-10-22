using System.Text.Json;
using Kiosk.Application.Auditoria;
using Kiosk.Application.CasosUso.Whatsapp;
using Kiosk.Application.Intenciones;
using Kiosk.Application.Puertos;
using Kiosk.Application.Puertos.Integraciones;
using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Auditoria;
using Kiosk.Domain.Common;
using Kiosk.Domain.Whatsapp;

namespace Kiosk.Application.CasosUso.Chat;

public sealed class PipelineConversacional
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

    private readonly IIntencionRepository _intenciones;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguracionRepository _config;
    private readonly IAuditoriaRepository _auditoria;
    private readonly IIaParser _parser;
    private readonly ResolvedorCatalogos _resolvedor;
    private readonly EjecutorAcciones _ejecutor;
    private readonly VerificadorEfectoAplicado _verificador;
    private readonly IStockLedger _stockLedger;

    public PipelineConversacional(
        IIntencionRepository intenciones,
        IUnitOfWork unitOfWork,
        IConfiguracionRepository config,
        IAuditoriaRepository auditoria,
        IIaParser parser,
        ResolvedorCatalogos resolvedor,
        EjecutorAcciones ejecutor,
        VerificadorEfectoAplicado verificador,
        IStockLedger stockLedger)
    {
        _intenciones = intenciones;
        _unitOfWork = unitOfWork;
        _config = config;
        _auditoria = auditoria;
        _parser = parser;
        _resolvedor = resolvedor;
        _ejecutor = ejecutor;
        _verificador = verificador;
        _stockLedger = stockLedger;
    }

    public async Task<string> ProcesarAsync(
        Guid comercioId,
        Canal canal,
        string canalUsuarioId,
        string texto,
        bool fueAudio,
        CancellationToken cancellationToken)
    {
        Intencion? intencion = null;

        try
        {
            var textoNormalizado = Normalizacion.Normalizar(texto);
            if (string.IsNullOrWhiteSpace(textoNormalizado))
            {
                var nombreVacio = await ObtenerNombreBotAsync(comercioId, cancellationToken);
                return RespuestasBot.NoInterpretado(nombreVacio);
            }

            var reconciliacion = await ReconciliarEjecutandoAsync(comercioId, canal, canalUsuarioId, cancellationToken);
            if (reconciliacion is not null && EsRespuestaConfirmacion(textoNormalizado))
            {
                return reconciliacion;
            }

            if (EsSaludo(textoNormalizado))
            {
                return await ObtenerBienvenidaAsync(comercioId, cancellationToken);
            }

            var pendiente = await _intenciones.GetPendienteAsync(comercioId, canal, canalUsuarioId, cancellationToken);
            if (pendiente is not null && pendiente.Estado == EstadoIntencion.ESPERANDO_CONFIRMACION)
            {
                if (pendiente.ConfirmacionExpirada(DateTime.UtcNow))
                {
                    pendiente.Cancelar("Timeout de confirmación expirado");
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    return RespuestasBot.TimeoutExpirado;
                }

                if (EsRespuestaConfirmacion(textoNormalizado))
                {
                    var respuesta = await ProcesarConfirmacionAsync(comercioId, canal, pendiente, textoNormalizado, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    return respuesta;
                }

                pendiente.Cancelar("Reemplazada por una nueva intención");
            }
            else if (pendiente is not null)
            {
                pendiente.Cancelar("Reemplazada por una nueva intención");
            }

            intencion = Intencion.Recibir(comercioId, canal, canalUsuarioId, texto, fueAudio);
            _intenciones.Add(intencion);
            AuditoriaRegistrador.Registrar(_auditoria, comercioId, canal, canalUsuarioId,
                AuditoriaTipos.IntencionRecibida, new { intencion.Id, texto, fueAudio });
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var parseo = await _parser.ParsearAsync(textoNormalizado, cancellationToken);
            var respuestaFinal = await DecidirAsync(comercioId, canal, intencion, parseo, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return respuestaFinal;
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
                }
            }

            return RespuestasBot.Error;
        }
    }

    private async Task<string> DecidirAsync(Guid comercioId, Canal canal, Intencion intencion, ResultadoParseo parseo, CancellationToken ct)
    {
        var canalUsuarioId = intencion.CanalUsuarioId;

        if (parseo.EsMultiComando)
        {
            intencion.Rechazar(parseo.Motivo ?? "Más de un comando en un solo mensaje");
            AuditoriaRegistrador.Registrar(_auditoria, comercioId, canal, canalUsuarioId,
                AuditoriaTipos.IntencionRechazada, new { intencion.Id, motivo = parseo.Motivo });
            return RespuestasBot.MultiComando;
        }

        if (parseo.EsFallo)
        {
            var nombreBot = await ObtenerNombreBotAsync(comercioId, ct);
            intencion.Rechazar(parseo.Motivo ?? "Mensaje no interpretable");
            AuditoriaRegistrador.Registrar(_auditoria, comercioId, canal, canalUsuarioId,
                AuditoriaTipos.IntencionRechazada, new { intencion.Id, motivo = parseo.Motivo });
            return RespuestasBot.NoInterpretado(nombreBot);
        }

        var comando = parseo.Comando!;
        intencion.MarcarParseada(JsonSerializer.Serialize(comando));

        if (!comando.TieneConfianzaSuficiente)
        {
            var nombreBot = await ObtenerNombreBotAsync(comercioId, ct);
            intencion.PedirAclaracion($"Confianza {comando.Confianza:P0} inferior al umbral");
            AuditoriaRegistrador.Registrar(_auditoria, comercioId, canal, canalUsuarioId,
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
            AuditoriaRegistrador.Registrar(_auditoria, comercioId, canal, canalUsuarioId,
                AuditoriaTipos.IntencionAclaracion, new { intencion.Id, faltantes, ambiguos });
            return RespuestasBot.FaltanCampos(faltantes, ambiguos);
        }

        var resolucion = await _resolvedor.ResolverAsync(comercioId, comando, ct);

        if (resolucion.Estado == EstadoResolucion.NO_ENCONTRADO)
        {
            intencion.Rechazar(resolucion.Motivo ?? "No se encontró el objetivo en el catálogo.");
            AuditoriaRegistrador.Registrar(_auditoria, comercioId, canal, canalUsuarioId,
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
            AuditoriaRegistrador.Registrar(_auditoria, comercioId, canal, canalUsuarioId,
                AuditoriaTipos.IntencionAclaracion, new { intencion.Id, presentaciones });
            return RespuestasBot.ElegiPresentacion(comando.Parametros.Producto ?? "", presentaciones);
        }

        if (comando.EsDestructivo)
        {
            var minutos = await ObtenerEnteroAsync(comercioId, ClavesConfiguracion.BotConfirmacionMinutos, 2, ct);
            var expira = DateTime.UtcNow.AddMinutes(Math.Max(1, minutos));
            intencion.PedirConfirmacion(expira);

            var mensajeConfirmacion = await ConfirmacionDestructivaAsync(comercioId, comando, resolucion.Coincidencia, ct);
            AuditoriaRegistrador.Registrar(_auditoria, comercioId, canal, canalUsuarioId,
                AuditoriaTipos.IntencionConfirmacion, new { intencion.Id, expiraEn = expira, comando.Accion });
            return mensajeConfirmacion;
        }

        return await EjecutarYResponderAsync(comercioId, canal, intencion, comando, resolucion.Coincidencia, ct);
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
        Canal canal,
        Intencion intencion,
        StructuredCommand comando,
        CoincidenciaPresentacion? objetivo,
        CancellationToken ct)
    {
        var resultado = await _ejecutor.EjecutarAsync(comercioId, comando, objetivo, intencion.CanalUsuarioId, ct);

        if (!resultado)
        {
            intencion.MarcarError(resultado.Error?.Message ?? "Error de ejecución");
            AuditoriaRegistrador.Registrar(_auditoria, comercioId, canal, intencion.CanalUsuarioId,
                AuditoriaTipos.IntencionError, new { intencion.Id, motivo = resultado.Error?.Message });
            return RespuestasBot.ErrorConMotivo(resultado.Error?.Message ?? "error desconocido");
        }

        intencion.Ejecutar(JsonSerializer.Serialize(new { respuesta = resultado.Value }));
        AuditoriaRegistrador.Registrar(_auditoria, comercioId, canal, intencion.CanalUsuarioId,
            AuditoriaTipos.IntencionEjecutada, new { intencion.Id, comando.Accion });
        return resultado.Value!;
    }

    private async Task<string> ProcesarConfirmacionAsync(
        Guid comercioId,
        Canal canal,
        Intencion pendiente,
        string textoNormalizado,
        CancellationToken ct)
    {
        if (Confirmaciones.Contains(textoNormalizado))
        {
            var comando = DeserializarComando(pendiente.StructuredCommandJson);
            var reclamo = await _intenciones.TryReclamarAsync(
                pendiente.Id, comercioId, canal, pendiente.CanalUsuarioId, ct);

            if (reclamo != ResultadoReclamo.Reclamado)
            {
                return reclamo switch
                {
                    ResultadoReclamo.EnEjecucion => RespuestasBot.OperacionEnProceso,
                    ResultadoReclamo.YaAplicada => RespuestasBot.OperacionYaAplicada,
                    _ => RespuestasBot.OperacionNoResuelta
                };
            }

            pendiente.MarcarEjecutando();

            var resolucion = await _resolvedor.ResolverAsync(comercioId, comando, ct);

            if (resolucion.Estado is not (EstadoResolucion.OK or EstadoResolucion.NO_APLICA))
            {
                pendiente.Cancelar($"No se pudo ejecutar: {resolucion.Motivo}");
                AuditoriaRegistrador.Registrar(_auditoria, comercioId, canal, pendiente.CanalUsuarioId,
                    AuditoriaTipos.IntencionCancelada, new { pendiente.Id, motivo = resolucion.Motivo });
                return RespuestasBot.CanceladoPorError(resolucion.Motivo ?? "el objetivo ya no existe");
            }

            var resultado = await _ejecutor.EjecutarAsync(comercioId, comando, resolucion.Coincidencia, pendiente.CanalUsuarioId, ct);

            if (!resultado)
            {
                pendiente.MarcarError(resultado.Error?.Message ?? "Error de ejecución");
                AuditoriaRegistrador.Registrar(_auditoria, comercioId, canal, pendiente.CanalUsuarioId,
                    AuditoriaTipos.IntencionError, new { pendiente.Id, motivo = resultado.Error?.Message });
                return RespuestasBot.ErrorConMotivo(resultado.Error?.Message ?? "error desconocido");
            }

            pendiente.Ejecutar(JsonSerializer.Serialize(new { respuesta = resultado.Value }));
            AuditoriaRegistrador.Registrar(_auditoria, comercioId, canal, pendiente.CanalUsuarioId,
                AuditoriaTipos.IntencionConfirmacion, new { pendiente.Id, confirmado = true });
            AuditoriaRegistrador.Registrar(_auditoria, comercioId, canal, pendiente.CanalUsuarioId,
                AuditoriaTipos.IntencionEjecutada, new { pendiente.Id, comando.Accion });
            return resultado.Value!;
        }

        pendiente.Cancelar("Usuario canceló la confirmación");
        AuditoriaRegistrador.Registrar(_auditoria, comercioId, canal, pendiente.CanalUsuarioId,
            AuditoriaTipos.IntencionCancelada, new { pendiente.Id, confirmado = false });
        return RespuestasBot.OperacionCancelada;
    }

    private async Task<string?> ReconciliarEjecutandoAsync(
        Guid comercioId,
        Canal canal,
        string canalUsuarioId,
        CancellationToken ct)
    {
        var ejecutando = await _intenciones.GetEjecutandoAsync(comercioId, canal, canalUsuarioId, ct);
        if (ejecutando.Count == 0)
        {
            return null;
        }

        var segundos = await ObtenerEnteroAsync(comercioId, ClavesConfiguracion.BotTimeoutEjecucionSegundos, 60, ct);
        var timeout = TimeSpan.FromSeconds(Math.Max(0, segundos));
        var ahora = DateTime.UtcNow;
        string? mensaje = null;
        var cambio = false;

        for (var i = 0; i < ejecutando.Count; i++)
        {
            var intencion = ejecutando[i];
            var esLaMasReciente = i == 0;

            if (!intencion.EjecucionVencida(ahora, timeout))
            {
                if (esLaMasReciente)
                {
                    mensaje = RespuestasBot.OperacionEnProceso;
                }

                continue;
            }

            var comando = IntentarDeserializarComando(intencion.StructuredCommandJson);
            var aplicada = comando is not null
                && await _verificador.YaAplicadoAsync(comercioId, comando, ct);

            if (aplicada)
            {
                intencion.Ejecutar(JsonSerializer.Serialize(new { reconciliada = true, resultado = "aplicada" }));
                AuditoriaRegistrador.Registrar(_auditoria, comercioId, canal, canalUsuarioId,
                    AuditoriaTipos.IntencionReconciliada, new { intencion.Id, resultado = "aplicada" });

                if (esLaMasReciente)
                {
                    mensaje = RespuestasBot.OperacionReconciliadaAplicada;
                }
            }
            else
            {
                intencion.MarcarError("No se pudo verificar si la operación se aplicó; revisá el catálogo.");
                AuditoriaRegistrador.Registrar(_auditoria, comercioId, canal, canalUsuarioId,
                    AuditoriaTipos.IntencionReconciliada, new { intencion.Id, resultado = "no_verificable" });

                if (esLaMasReciente)
                {
                    mensaje = RespuestasBot.OperacionReconciliadaNoVerificable;
                }
            }

            cambio = true;
        }

        if (cambio)
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }

        return mensaje;
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

    private static StructuredCommand? IntentarDeserializarComando(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<StructuredCommand>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
