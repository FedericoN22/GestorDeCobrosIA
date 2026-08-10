using Kiosk.Application.Intenciones;

namespace Kiosk.Application.Puertos.Integraciones;

public sealed record ResultadoParseo(
    StructuredCommand? Comando,
    bool EsMultiComando,
    string? Motivo)
{
    public bool EsFallo => Comando is null && !EsMultiComando;

    public static ResultadoParseo Ok(StructuredCommand comando) => new(comando, false, null);

    public static ResultadoParseo MultiComando(string motivo) => new(null, true, motivo);

    public static ResultadoParseo Fallo(string motivo) => new(null, false, motivo);
}
