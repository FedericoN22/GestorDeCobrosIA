using Kiosk.Application.CasosUso.Stock;
using Kiosk.Application.Tests.TestDoubles;
using Kiosk.Domain.Catalogos;
using Kiosk.Domain.Common;
using Kiosk.Domain.Stock;

namespace Kiosk.Application.Tests;

public class ServicioStockTests
{
    private static readonly Guid ComercioId = Guid.NewGuid();

    private static (ServicioStock Servicio, FakeProductRepository Productos, FakeStockLedger Ledger, FakeUnitOfWork Uow, Presentacion Presentacion) CrearServicio(
        int stockInicial = 0)
    {
        var productos = new FakeProductRepository();
        var producto = Producto.Crear(ComercioId, null, "Coca Cola");
        var presentacion = producto.AgregarPresentacion("1.5L", 1500);
        productos.Seed(producto);

        var ledger = new FakeStockLedger();
        if (stockInicial > 0)
        {
            ledger.Seed(MovimientoStock.EntradaManual(presentacion.Id, stockInicial, null, Canal.WEB));
        }

        var uow = new FakeUnitOfWork(() => ledger.Commit());
        var servicio = new ServicioStock(productos, ledger, new FakeAuditoriaRepository(), uow);

        return (servicio, productos, ledger, uow, presentacion);
    }

    [Fact]
    public async Task EntradaManual_ConStockPrevio10_Agrega7_ResultadoStock17()
    {
        var (servicio, _, ledger, _, presentacion) = CrearServicio(stockInicial: 10);

        var resultado = await servicio.EntradaManualAsync(new EntradaManualCommand(
            ComercioId, presentacion.Id, 7, null, "whatsapp", Canal.WHATSAPP));

        Assert.True(resultado.IsSuccess);
        Assert.Equal(17, resultado.Value!.StockActual);
        Assert.Equal(17, presentacion.StockActual);
        Assert.Equal(17, await ledger.CalcularStockAsync(presentacion.Id));
    }

    [Fact]
    public async Task EntradaManual_PresentacionInexistente_Falla()
    {
        var (servicio, _, _, _, _) = CrearServicio();

        var resultado = await servicio.EntradaManualAsync(new EntradaManualCommand(
            ComercioId, Guid.NewGuid(), 7, null, "whatsapp", Canal.WHATSAPP));

        Assert.False(resultado.IsSuccess);
        Assert.Equal("PRESENTACION_NO_ENCONTRADA", resultado.Error?.Code);
    }

    [Fact]
    public async Task EntradaManual_ConPrecioCosto_ActualizaPrecioCosto()
    {
        var (servicio, _, _, _, presentacion) = CrearServicio();

        var resultado = await servicio.EntradaManualAsync(new EntradaManualCommand(
            ComercioId, presentacion.Id, 5, null, "whatsapp", Canal.WHATSAPP, 4200));

        Assert.True(resultado.IsSuccess);
        Assert.Equal(4200, presentacion.PrecioCostoCentavos);
    }

    [Fact]
    public async Task Ajuste_Stock10_Menos3_Resultado7()
    {
        var (servicio, _, ledger, _, presentacion) = CrearServicio(stockInicial: 10);

        var resultado = await servicio.AjusteAsync(new AjusteStockCommand(
            ComercioId, presentacion.Id, -3, "Merma", null, "whatsapp", Canal.WHATSAPP));

        Assert.True(resultado.IsSuccess);
        Assert.Equal(7, resultado.Value!.StockActual);
        Assert.Equal(7, presentacion.StockActual);
        Assert.Equal(7, await ledger.CalcularStockAsync(presentacion.Id));
        Assert.Contains(ledger.Movimientos, m => m.Tipo == TipoMovimiento.AJUSTE && m.Cantidad == -3);
    }

    [Fact]
    public async Task Ajuste_DejariaNegativo_Rechazado_SinMovimientoNiCambioDeSnapshot()
    {
        var (servicio, _, ledger, _, presentacion) = CrearServicio(stockInicial: 10);
        var movimientosAntes = ledger.Movimientos.Count;

        var resultado = await servicio.AjusteAsync(new AjusteStockCommand(
            ComercioId, presentacion.Id, -11, "Merma", null, "whatsapp", Canal.WHATSAPP));

        Assert.False(resultado.IsSuccess);
        Assert.Equal("STOCK_NEGATIVO", resultado.Error?.Code);
        Assert.Equal(movimientosAntes, ledger.Movimientos.Count);
        Assert.Equal(0, presentacion.StockActual);
        Assert.Equal(10, await ledger.CalcularStockAsync(presentacion.Id));
    }

    [Fact]
    public async Task Ajuste_FalloDePersistencia_NoDejaMovimientoEnElLedger()
    {
        var (servicio, _, ledger, uow, presentacion) = CrearServicio(stockInicial: 10);
        uow.LanzarError = true;
        var movimientosAntes = ledger.Movimientos.Count;

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.AjusteAsync(
            new AjusteStockCommand(ComercioId, presentacion.Id, -3, "Merma", null, "whatsapp", Canal.WHATSAPP)));

        Assert.Equal(movimientosAntes, ledger.Movimientos.Count);
        Assert.Equal(10, await ledger.CalcularStockAsync(presentacion.Id));
    }

    [Fact]
    public async Task AjusteConcurrente_SobreMismoStock_NuncaQuedaNegativo()
    {
        var (servicio, _, ledger, _, presentacion) = CrearServicio(stockInicial: 10);

        var t1 = servicio.AjusteAsync(new AjusteStockCommand(ComercioId, presentacion.Id, -7, "Merma 1", null, "whatsapp", Canal.WHATSAPP));
        var t2 = servicio.AjusteAsync(new AjusteStockCommand(ComercioId, presentacion.Id, -7, "Merma 2", null, "whatsapp", Canal.WHATSAPP));
        var resultados = await Task.WhenAll(t1, t2);

        Assert.Equal(1, resultados.Count(r => r.IsSuccess));
        Assert.Equal(3, presentacion.StockActual);
        Assert.Equal(3, await ledger.CalcularStockAsync(presentacion.Id));
    }

    [Fact]
    public async Task ConfigurarStockMinimo_ActualizaYReportaStockBajo()
    {
        var (servicio, _, _, _, presentacion) = CrearServicio(stockInicial: 5);

        var resultado = await servicio.ConfigurarStockMinimoAsync(
            new ConfigurarStockMinimoCommand(ComercioId, presentacion.Id, 5, "whatsapp", Canal.WHATSAPP));

        Assert.True(resultado.IsSuccess);
        Assert.True(resultado.Value!.StockBajo);
        Assert.Equal(5, presentacion.StockMinimo);
    }
}
