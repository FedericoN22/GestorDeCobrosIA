using Kiosk.Domain.Common;

namespace Kiosk.Domain.Whatsapp;

public enum EstadoIntencion
{
    RECIBIDA = 1,
    PARSEADA = 2,
    ACLARACION = 3,
    ESPERANDO_CONFIRMACION = 4,
    EJECUTADA = 5,
    CANCELADA = 6,
    RECHAZADA = 7,
    ERROR = 8
}

public class Intencion
{
    public Guid Id { get; private set; }
    public Guid ComercioId { get; private set; }

    public Canal Canal { get; private set; }

    public string CanalUsuarioId { get; private set; } = null!;
    public string TextoOriginal { get; private set; } = null!;
    public string? StructuredCommandJson { get; private set; }
    public string? Decision { get; private set; }
    public string? ResultadoJson { get; private set; }
    
    public bool FueAudio { get; private set; }

    public DateTime? ExpiraEn { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public EstadoIntencion Estado { get; private set; }



    private Intencion() { }

    public static Intencion Recibir(Guid comercioId, Canal canal, string canalUsuarioId, string textoOriginal, bool fueAudio = false)
    {
        if (string.IsNullOrWhiteSpace(canalUsuarioId))
        {
            throw new DomainException("INTENCION_CANALUSUARIO_REQUERIDO", "El identificador de usuario para el canal es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(textoOriginal))
        {
            throw new DomainException("INTENCION_TEXTO_REQUERIDO", "El texto del mensaje es obligatorio.");
        }

        var ahora = DateTime.UtcNow;
        return new Intencion
        {
            Id = Guid.NewGuid(),
            ComercioId = comercioId,
            Canal = canal,
            CanalUsuarioId = canalUsuarioId.Trim(),
            TextoOriginal = textoOriginal,
            FueAudio = fueAudio,
            Estado = EstadoIntencion.RECIBIDA,
            CreatedAt = ahora,
            UpdatedAt = ahora
        };
    }

    public void MarcarParseada(string structuredCommandJson)
    {
        if (string.IsNullOrWhiteSpace(structuredCommandJson))
        {
            throw new DomainException("INTENCION_COMANDO_REQUERIDO", "El comando estructurado es obligatorio.");
        }

        StructuredCommandJson = structuredCommandJson;
        Estado = EstadoIntencion.PARSEADA;
        UpdatedAt = DateTime.UtcNow;
    }

    public void PedirConfirmacion(DateTime expiraEn)
    {
        if (Estado != EstadoIntencion.PARSEADA)
        {
            throw new DomainException("INTENCION_ESTADO_INVALIDO", "Solo una intención parseada puede pedir confirmación.");
        }

        Estado = EstadoIntencion.ESPERANDO_CONFIRMACION;
        ExpiraEn = expiraEn;
        UpdatedAt = DateTime.UtcNow;
    }

    public void PedirAclaracion(string decision)
    {
        if (Estado != EstadoIntencion.PARSEADA)
        {
            throw new DomainException("INTENCION_ESTADO_INVALIDO", "Solo una intención parseada puede pedir aclaración.");
        }

        Estado = EstadoIntencion.ACLARACION;
        Decision = decision;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Ejecutar(string resultadoJson)
    {
        if (Estado != EstadoIntencion.ESPERANDO_CONFIRMACION && Estado != EstadoIntencion.ACLARACION && Estado != EstadoIntencion.PARSEADA)
        {
            throw new DomainException("INTENCION_ESTADO_INVALIDO", "La intención no está en un estado que permita ejecutarla.");
        }

        Estado = EstadoIntencion.EJECUTADA;
        ResultadoJson = resultadoJson;
        ExpiraEn = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancelar(string decision = "Usuario canceló o expiró el timeout")
    {
        Estado = EstadoIntencion.CANCELADA;
        Decision = decision;
        ExpiraEn = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Rechazar(string decision)
    {
        Estado = EstadoIntencion.RECHAZADA;
        Decision = decision;
        ExpiraEn = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarcarError(string decision)
    {
        Estado = EstadoIntencion.ERROR;
        Decision = decision;
        ExpiraEn = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool ConfirmacionExpirada(DateTime ahora)
    {
        return Estado == EstadoIntencion.ESPERANDO_CONFIRMACION && ExpiraEn is not null && ExpiraEn <= ahora;
    }
}
