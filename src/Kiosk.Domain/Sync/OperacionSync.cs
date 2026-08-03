using Kiosk.Domain.Common;

namespace Kiosk.Domain.Sync;

public class OperacionSync
{
    public Guid Id { get; private set; }
    public Guid ComercioId { get; private set; }
    public Guid OperationId { get; private set; }
    public string Tipo { get; private set; } = null!;
    public string? ResultadoJson { get; private set; }
    public DateTime AplicadaEn { get; private set; }
    public DateTime? ConfirmadaEn { get; private set; }

    private OperacionSync() { }

    public static OperacionSync Registrar(Guid comercioId, Guid operationId, string tipo, string resultadoJson)
    {
        if (operationId == Guid.Empty)
        {
            throw new DomainException("OPERATION_ID_INVALIDO", "El operationId de la operación es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(tipo))
        {
            throw new DomainException("OPERACION_TIPO_REQUERIDO", "El tipo de la operación es obligatorio.");
        }

        return new OperacionSync
        {
            Id = Guid.NewGuid(),
            ComercioId = comercioId,
            OperationId = operationId,
            Tipo = tipo.Trim(),
            ResultadoJson = resultadoJson,
            AplicadaEn = DateTime.UtcNow
        };
    }

    public void Confirmar()
    {
        ConfirmadaEn = DateTime.UtcNow;
    }
}
