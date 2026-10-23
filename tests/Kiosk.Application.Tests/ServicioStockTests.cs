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

    [Fact]
    public async Task EntradaManual_Idempotencia_PrimeraEjecucion_AplicaMovimientoUnaVez()
    {
        var (servicio, _, ledger, _, presentacion) = CrearServicio();
        var clave = MovimientoStock.GenerarClaveIdempotencia(Guid.NewGuid(), "AGREGAR_STOCK", presentacion.Id, 10, null);

        var resultado = await servicio.EntradaManualAsync(new EntradaManualCommand(
            ComercioId, presentacion.Id, 10, null, "whatsapp", Canal.WHATSAPP, null, clave));

        Assert.True(resultado.IsSuccess);
        Assert.Equal(10, resultado.Value!.StockActual);
        Assert.Single(ledger.Movimientos);
        Assert.Equal(clave, ledger.Movimientos[0].ClaveIdempotencia);
    }

    [Fact]
    public async Task EntradaManual_Idempotencia_RepeticionSecuencial_MismoResultadoSinNuevoMovimiento()
    {
        var (servicio, _, ledger, _, presentacion) = CrearServicio();
        var clave = MovimientoStock.GenerarClaveIdempotencia(Guid.NewGuid(), "AGREGAR_STOCK", presentacion.Id, 10, null);

        var r1 = await servicio.EntradaManualAsync(new EntradaManualCommand(
            ComercioId, presentacion.Id, 10, null, "whatsapp", Canal.WHATSAPP, null, clave));

        var r2 = await servicio.EntradaManualAsync(new EntradaManualCommand(
            ComercioId, presentacion.Id, 10, null, "whatsapp", Canal.WHATSAPP, null, clave));

        Assert.True(r1.IsSuccess);
        Assert.True(r2.IsSuccess);
        Assert.Equal(r1.Value!.StockActual, r2.Value!.StockActual);
        Assert.Single(ledger.Movimientos);
        Assert.Equal(10, await ledger.CalcularStockAsync(presentacion.Id));
    }

    [Fact]
    public async Task EntradaManual_Idempotencia_ConcurrenciaMismaClave_SoloUnaSeAplica()
    {
        var (servicio, _, ledger, _, presentacion) = CrearServicio();
        var clave = MovimientoStock.GenerarClaveIdempotencia(Guid.NewGuid(), "AGREGAR_STOCK", presentacion.Id, 10, null);

        var t1 = servicio.EntradaManualAsync(new EntradaManualCommand(
            ComercioId, presentacion.Id, 10, null, "whatsapp", Canal.WHATSAPP, null, clave));
        var t2 = servicio.EntradaManualAsync(new EntradaManualCommand(
            ComercioId, presentacion.Id, 10, null, "whatsapp", Canal.WHATSAPP, null, clave));

        var resultados = await Task.WhenAll(t1, t2);

        // Ambas deben devolver éxito (idempotencia: reintento devuelve resultado original)
        Assert.Equal(2, resultados.Count(r => r.IsSuccess));
        Assert.Equal(10, resultados.First(r => r.IsSuccess).Value!.StockActual);
        // Pero solo se crea UN movimiento en el ledger
        Assert.Single(ledger.Movimientos);
        Assert.Equal(10, await ledger.CalcularStockAsync(presentacion.Id));
    }

    [Fact]
    public async Task EntradaManual_Idempotencia_ReintentoPostPersistencia_DevuelveResultadoOriginal()
    {
        var (servicio, _, ledger, uow, presentacion) = CrearServicio();
        var clave = MovimientoStock.GenerarClaveIdempotencia(Guid.NewGuid(), "AGREGAR_STOCK", presentacion.Id, 10, null);

        var r1 = await servicio.EntradaManualAsync(new EntradaManualCommand(
            ComercioId, presentacion.Id, 10, null, "whatsapp", Canal.WHATSAPP, null, clave));

        uow.LanzarError = true;

        var r2 = await servicio.EntradaManualAsync(new EntradaManualCommand(
            ComercioId, presentacion.Id, 10, null, "whatsapp", Canal.WHATSAPP, null, clave));

        uow.LanzarError = false;

        var r3 = await servicio.EntradaManualAsync(new EntradaManualCommand(
            ComercioId, presentacion.Id, 10, null, "whatsapp", Canal.WHATSAPP, null, clave));

        Assert.True(r1.IsSuccess);
        Assert.True(r2.IsSuccess);
        Assert.True(r3.IsSuccess);
        Assert.Equal(r1.Value!.StockActual, r2.Value!.StockActual);
        Assert.Equal(r1.Value!.StockActual, r3.Value!.StockActual);
        Assert.Single(ledger.Movimientos);
        Assert.Equal(10, await ledger.CalcularStockAsync(presentacion.Id));
    }

    [Fact]
    public async Task EntradaManual_Idempotencia_MismaClaveDatosIncompatibles_FallaConflicto()
    {
        var (servicio, _, ledger, _, presentacion) = CrearServicio();
        var clave = MovimientoStock.GenerarClaveIdempotencia(Guid.NewGuid(), "AGREGAR_STOCK", presentacion.Id, 10, null);

        var r1 = await servicio.EntradaManualAsync(new EntradaManualCommand(
            ComercioId, presentacion.Id, 10, null, "whatsapp", Canal.WHATSAPP, null, clave));

        Assert.True(r1.IsSuccess);

        var r2 = await servicio.EntradaManualAsync(new EntradaManualCommand(
            ComercioId, presentacion.Id, 20, null, "whatsapp", Canal.WHATSAPP, null, clave));

        Assert.False(r2.IsSuccess);
        Assert.Equal("IDEMPOTENCIA_CONFLICTO", r2.Error?.Code);
        Assert.Single(ledger.Movimientos);
        Assert.Equal(10, await ledger.CalcularStockAsync(presentacion.Id));
    }

    [Fact]
    public async Task EntradaManual_Idempotencia_ClavesDiferentesIntencionesDiferentes_AmbasSeAplican()
    {
        var (servicio, _, ledger, _, presentacion) = CrearServicio();
        var clave1 = MovimientoStock.GenerarClaveIdempotencia(Guid.NewGuid(), "AGREGAR_STOCK", presentacion.Id, 10, null);
        var clave2 = MovimientoStock.GenerarClaveIdempotencia(Guid.NewGuid(), "AGREGAR_STOCK", presentacion.Id, 5, null);

        var r1 = await servicio.EntradaManualAsync(new EntradaManualCommand(
            ComercioId, presentacion.Id, 10, null, "whatsapp", Canal.WHATSAPP, null, clave1));

        var r2 = await servicio.EntradaManualAsync(new EntradaManualCommand(
            ComercioId, presentacion.Id, 5, null, "whatsapp", Canal.WHATSAPP, null, clave2));

        Assert.True(r1.IsSuccess);
        Assert.True(r2.IsSuccess);
        Assert.Equal(15, r2.Value!.StockActual);
        Assert.Equal(2, ledger.Movimientos.Count);
        Assert.Equal(15, await ledger.CalcularStockAsync(presentacion.Id));
    }

    [Fact]
    public async Task EntradaManual_Idempotencia_CompatibilidadMovimientosHistoricos_SinClaveFuncionaNormal()
    {
        var (servicio, _, ledger, _, presentacion) = CrearServicio(stockInicial: 5);

        var r1 = await servicio.EntradaManualAsync(new EntradaManualCommand(
            ComercioId, presentacion.Id, 3, null, "whatsapp", Canal.WHATSAPP));

        Assert.True(r1.IsSuccess);
        Assert.Equal(8, r1.Value!.StockActual);
        Assert.Equal(8, await ledger.CalcularStockAsync(presentacion.Id));

        Assert.Contains(ledger.Movimientos, m => m.Cantidad == 5);
        Assert.Contains(ledger.Movimientos, m => m.Cantidad == 3 && m.ClaveIdempotencia is null);
        Assert.Equal(2, ledger.Movimientos.Count);
    }
}
