using Kiosk.Application.Intenciones;
using Kiosk.Application.Puertos.Repositorios;

namespace Kiosk.Application.CasosUso.Whatsapp;

public sealed class ConfirmationPolicy
{
    public const int DefaultMaxCantidadIncremento = 1000;
    public const int UmbralConfirmacionAgregarStock = 200;
    public const int MaxPrecioVentaCentavos = 10_000_000 * 100;

    private readonly IConfiguracionRepository _config;

    public ConfirmationPolicy(IConfiguracionRepository config)
    {
        _config = config;
    }

    public async Task<bool> RequiereConfirmacionAsync(
        Guid comercioId,
        StructuredCommand comando,
        CoincidenciaPresentacion? objetivo,
        CancellationToken ct)
    {
        return comando.Accion switch
        {
            AccionIntencion.CREAR_PRODUCTO => true,
            AccionIntencion.MODIFICAR_PRECIO => true,
            AccionIntencion.ELIMINAR_PRODUCTO => true,
            AccionIntencion.AGREGAR_STOCK => await RequiereConfirmacionAgregarStockAsync(comercioId, comando, ct),
            _ => false
        };
    }

    private async Task<bool> RequiereConfirmacionAgregarStockAsync(
        Guid comercioId,
        StructuredCommand comando,
        CancellationToken ct)
    {
        var cantidad = comando.Parametros.Cantidad ?? 0;
        return cantidad > UmbralConfirmacionAgregarStock;
    }

    public async Task<int> ObtenerMaxCantidadPermitidaAsync(Guid comercioId, CancellationToken ct)
    {
        var maxConfig = await ObtenerMaxCantidadConfigAsync(comercioId, ct);
        return Math.Min(DefaultMaxCantidadIncremento, maxConfig);
    }

    private async Task<int> ObtenerMaxCantidadConfigAsync(Guid comercioId, CancellationToken ct)
    {
        var valor = await _config.GetAsync(comercioId, ClavesConfiguracion.BotStockCantidadMaxima, ct);
        if (int.TryParse(valor?.Valor, out var configurado) && configurado > 0)
        {
            return configurado;
        }
        return DefaultMaxCantidadIncremento;
    }

    public bool ValidarLimiteCantidad(int cantidad, int maxPermitido)
        => cantidad > 0 && cantidad <= maxPermitido;

    public bool ValidarPrecioVenta(int precioCentavos)
        => precioCentavos > 0 && precioCentavos <= MaxPrecioVentaCentavos;
}