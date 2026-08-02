namespace Kiosk.Application.Abstractions;

public sealed record Error(string Code, string Message)
{
    public static Error NoEncontrado(string entidad) =>
        new("NO_ENCONTRADO", $"No se encontró la entidad '{entidad}'.");
}
