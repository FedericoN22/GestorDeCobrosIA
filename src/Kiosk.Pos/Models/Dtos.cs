using System.Text.Json;

namespace Kiosk.Pos.Models;

public sealed record UsuarioDto(
    Guid Id,
    string Username,
    string Nombre,
    string Rol,
    Guid ComercioId,
    IReadOnlyCollection<string> Permisos);

public sealed record LoginResponseDto(string Token, DateTime ExpiraEn, UsuarioDto Usuario);

public sealed record CajaResponseDto(
    Guid Id,
    DateTime FechaApertura,
    int MontoInicialCentavos,
    int Estado,
    DateTime? FechaCierre,
    int? MontoEsperadoCentavos,
    int? MontoDeclaradoCentavos,
    int? DiferenciaCentavos);

public sealed record CategoriaDto(Guid Id, Guid ComercioId, string Nombre, bool Activa);

public sealed record PresentacionDto(
    Guid Id,
    Guid ProductoId,
    string Nombre,
    string? CodigoBarras,
    int PrecioVentaCentavos,
    int? PrecioCostoCentavos,
    bool Activa,
    int StockActual,
    int? StockMinimo);

public sealed record ProductoDto(
    Guid Id,
    Guid ComercioId,
    Guid? CategoriaId,
    string Nombre,
    string NombreNormalizado,
    bool Activo,
    IReadOnlyList<PresentacionDto> Presentaciones);

public sealed record EstadoSyncDto(
    DateTime Cursor,
    IReadOnlyList<CategoriaDto> Categorias,
    IReadOnlyList<ProductoDto> Productos);

public sealed record ResultadoBusqueda(
    Guid PresentacionId,
    Guid ProductoId,
    string ProductoNombre,
    string PresentacionNombre,
    string? CodigoBarras,
    int PrecioVentaCentavos,
    int StockActual);

public sealed record OperacionBatchDto(Guid OperationId, string Tipo, JsonElement? Payload);

public sealed record ResultadoOperacionDto(Guid OperationId, bool Ok, JsonElement? Resultado, string? Error, string? Message);

public sealed record ProcesarBatchResultDto(IReadOnlyList<ResultadoOperacionDto> Resultados);

public sealed record ConfirmarSyncDto(int Confirmadas);

public enum EstadoCajaDto
{
    Abierta = 1,
    Cerrada = 2
}
