using Kiosk.Domain.Ventas;

namespace Kiosk.Pos.Models;

public sealed class Sesion
{
    public required string Token { get; init; }
    public required Guid ComercioId { get; init; }
    public required Guid UsuarioId { get; init; }
    public required string Username { get; init; }
    public required string Nombre { get; init; }
    public required string Rol { get; init; }
    public DateTime LoginEn { get; init; } = DateTime.UtcNow;
}

public sealed class CajaLocal
{
    public Guid Id { get; set; }
    public Guid ComercioId { get; set; }
    public Guid UsuarioId { get; set; }
    public DateTime FechaApertura { get; set; }
    public int MontoInicialCentavos { get; set; }
    public EstadoCaja Estado { get; set; }
    public DateTime? FechaCierre { get; set; }
    public int? MontoEsperadoCentavos { get; set; }
    public int? MontoDeclaradoCentavos { get; set; }
    public int? DiferenciaCentavos { get; set; }
}

public sealed class PendingOp
{
    public long Id { get; set; }
    public Guid OperationId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string Estado { get; set; } = "PENDIENTE";
    public string? Error { get; set; }
    public DateTime CreadaEn { get; set; }
    public DateTime? ConfirmadaEn { get; set; }
}

public sealed class LineaLocal
{
    public required Guid PresentacionId { get; init; }
    public required string ProductoNombre { get; init; }
    public required string PresentacionNombre { get; init; }
    public required int Cantidad { get; init; }
    public required int PrecioUnitarioCentavos { get; init; }
    public int SubtotalCentavos => Cantidad * PrecioUnitarioCentavos;
}

public sealed class PagoLocal
{
    public required MedioPago Medio { get; init; }
    public required int MontoCentavos { get; init; }
}

public sealed class VentaLocal
{
    public Guid Id { get; set; }
    public int Numero { get; set; }
    public Guid CajaId { get; set; }
    public int TotalCentavos { get; set; }
    public DateTime Fecha { get; set; }
    public bool ClientGenerated { get; set; }
    public List<LineaLocal> Lineas { get; set; } = [];
    public List<PagoLocal> Pagos { get; set; } = [];
}
