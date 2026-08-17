using System.Text.Json;
using Kiosk.Application.CasosUso.Catalogos;
using Kiosk.Application.CasosUso.Stock;
using Kiosk.Application.CasosUso.Whatsapp;
using Kiosk.Application.Intenciones;
using Kiosk.Application.Puertos.Integraciones;
using Kiosk.Application.Tests.TestDoubles;
using Kiosk.Domain.Auditoria;
using Kiosk.Domain.Catalogos;
using Kiosk.Domain.Common;
using Kiosk.Domain.Stock;
using Kiosk.Domain.Whatsapp;

namespace Kiosk.Application.Tests;

public class ServicioWhatsAppTests
{
    private static readonly Guid ComercioId = Guid.NewGuid();
    private const string Numero = "5491100000000";

    private sealed class Contexto
    {
        public Contexto()
        {
            Uow = new FakeUnitOfWork(() => Ledger.Commit());

            var stock = new ServicioStock(Productos, Ledger, Auditoria, Uow);
            var productos = new ServicioProductos(Productos, Categorias, Auditoria, Uow);
            var resolvedor = new ResolvedorCatalogos(Productos);
            var ejecutor = new EjecutorAcciones(stock, productos, resolvedor, Ledger);

            Servicio = new ServicioWhatsApp(
                Whitelist, Intenciones, Uow, Config, Auditoria, Parser, Sender,
                resolvedor, ejecutor, new RateLimiterWhatsApp(), Ledger);
        }

        public FakeWhatsAppWhitelistRepository Whitelist { get; } = new();
        public FakeIntencionRepository Intenciones { get; } = new();
        public FakeUnitOfWork Uow { get; }
        public FakeConfiguracionRepository Config { get; } = new();
        public FakeAuditoriaRepository Auditoria { get; } = new();
        public FakeParser Parser { get; } = new();
        public FakeWhatsAppSender Sender { get; } = new();
        public FakeProductRepository Productos { get; } = new();
        public FakeCategoriaRepository Categorias { get; } = new();
        public FakeStockLedger Ledger { get; } = new();
        public ServicioWhatsApp Servicio { get; }
    }

    private static StructuredCommand Comando(
        AccionIntencion accion,
        decimal confianza,
        string? producto = null,
        string? presentacion = null,
        int? cantidad = null,
        int? precio = null,
        IReadOnlyList<string>? faltantes = null,
        IReadOnlyList<string>? ambiguos = null)
        => new(
            1,
            accion,
            presentacion is null ? "PRODUCTO" : "PRESENTACION",
            new ParametrosComando(producto, presentacion, cantidad, precio, TipoPrecio.NO_INDICADO, null, producto),
            confianza,
            faltantes ?? [],
            ambiguos ?? [],
            producto ?? "");

    private static Presentacion SeedProducto(Contexto ctx, string nombreProducto, string nombrePresentacion)
    {
        var producto = Producto.Crear(ComercioId, null, nombreProducto);
        var presentacion = producto.AgregarPresentacion(nombrePresentacion, 1500);
        ctx.Productos.Seed(producto);
        return presentacion;
    }

    private static Intencion SeedConfirmacionPendiente(Contexto ctx, AccionIntencion accion, string producto, string presentacion)
    {
        var comando = Comando(accion, 0.95m, producto, presentacion);
        var pendiente = Intencion.Recibir(ComercioId, Numero, "mensaje original");
        pendiente.MarcarParseada(JsonSerializer.Serialize(comando));
        pendiente.PedirConfirmacion(DateTime.UtcNow.AddMinutes(5));
        ctx.Intenciones.Seed(pendiente);
        return pendiente;
    }

    [Fact]
    public async Task NumeroNoAutorizado_RespondeNoAutorizado_NoLlamaAlParser()
    {
        var ctx = new Contexto();

        var respuesta = await ctx.Servicio.ProcesarMensajeAsync(ComercioId, Numero, "HOLA");

        Assert.Equal(RespuestasBot.NoAutorizado, respuesta);
        Assert.Empty(ctx.Parser.Llamadas);
        Assert.Single(ctx.Sender.Enviados);
    }

    [Fact]
    public async Task Saludo_RespondeBienvenida_NoLlamaAlParser()
    {
        var ctx = new Contexto();
        ctx.Whitelist.Autorizar(ComercioId, Numero);
        ctx.Config.Set(ComercioId, ClavesConfiguracion.BotBienvenida, "Bienvenido!");
        ctx.Config.Set(ComercioId, ClavesConfiguracion.BotNombre, "KioscoBot");

        var respuesta = await ctx.Servicio.ProcesarMensajeAsync(ComercioId, Numero, "hola");

        Assert.Equal("Bienvenido!", respuesta);
        Assert.Empty(ctx.Parser.Llamadas);
    }

    [Fact]
    public async Task MensajeVacio_RespondeNoInterpretado()
    {
        var ctx = new Contexto();
        ctx.Whitelist.Autorizar(ComercioId, Numero);

        var respuesta = await ctx.Servicio.ProcesarMensajeAsync(ComercioId, Numero, "   ");

        Assert.Equal(RespuestasBot.NoInterpretado("asistente"), respuesta);
    }

    [Fact]
    public async Task ExcedeLimitePorMinuto_RespondeLimiteExcedido()
    {
        var ctx = new Contexto();
        ctx.Whitelist.Autorizar(ComercioId, Numero);
        ctx.Config.Set(ComercioId, ClavesConfiguracion.BotLimiteMensajesPorMinuto, "2");

        await ctx.Servicio.ProcesarMensajeAsync(ComercioId, Numero, "HOLA");
        await ctx.Servicio.ProcesarMensajeAsync(ComercioId, Numero, "HOLA");

        var respuesta = await ctx.Servicio.ProcesarMensajeAsync(ComercioId, Numero, "HOLA");

        Assert.Equal(RespuestasBot.LimiteExcedido, respuesta);
    }

    [Fact]
    public async Task ConfianzaBaja_PideAclaracion_NoEjecuta()
    {
        var ctx = new Contexto();
        ctx.Whitelist.Autorizar(ComercioId, Numero);
        ctx.Parser.Responder(ResultadoParseo.Ok(Comando(AccionIntencion.CONSULTAR_STOCK, 0.4m, "COCA")));

        var respuesta = await ctx.Servicio.ProcesarMensajeAsync(ComercioId, Numero, "chisme raro");

        Assert.Equal(RespuestasBot.ConfianzaBaja("asistente"), respuesta);
        var intencion = ctx.Intenciones.Intenciones.Single();
        Assert.Equal(EstadoIntencion.ACLARACION, intencion.Estado);
        var enviado = Assert.Single(ctx.Sender.Enviados);
        Assert.Equal(respuesta, enviado.Texto);
    }

    [Fact]
    public async Task MultiComando_RechazaLaIntencion()
    {
        var ctx = new Contexto();
        ctx.Whitelist.Autorizar(ComercioId, Numero);
        ctx.Parser.Responder(ResultadoParseo.MultiComando("más de una instrucción"));

        var respuesta = await ctx.Servicio.ProcesarMensajeAsync(ComercioId, Numero, "agregar x y eliminar y");

        Assert.Equal(RespuestasBot.MultiComando, respuesta);
        var intencion = ctx.Intenciones.Intenciones.Single();
        Assert.Equal(EstadoIntencion.RECHAZADA, intencion.Estado);
    }

    [Fact]
    public async Task MensajeNoInterpretable_RespondeNoInterpretado()
    {
        var ctx = new Contexto();
        ctx.Whitelist.Autorizar(ComercioId, Numero);
        ctx.Parser.Responder(ResultadoParseo.Fallo("no entendí"));

        var respuesta = await ctx.Servicio.ProcesarMensajeAsync(ComercioId, Numero, "asdfgh");

        Assert.Equal(RespuestasBot.NoInterpretado("asistente"), respuesta);
        var intencion = ctx.Intenciones.Intenciones.Single();
        Assert.Equal(EstadoIntencion.RECHAZADA, intencion.Estado);
    }

    [Fact]
    public async Task FaltanCampos_PideAclaracion()
    {
        var ctx = new Contexto();
        ctx.Whitelist.Autorizar(ComercioId, Numero);
        ctx.Parser.Responder(ResultadoParseo.Ok(Comando(
            AccionIntencion.AGREGAR_STOCK, 0.9m, "COCA", "1.5L", null, null, ["cantidad"])));

        var respuesta = await ctx.Servicio.ProcesarMensajeAsync(ComercioId, Numero, "agregar coca");

        Assert.Equal(RespuestasBot.FaltanCampos(["cantidad"], []), respuesta);
        var intencion = ctx.Intenciones.Intenciones.Single();
        Assert.Equal(EstadoIntencion.ACLARACION, intencion.Estado);
    }

    [Fact]
    public async Task ProductoNoEncontrado_RechazaYResponde()
    {
        var ctx = new Contexto();
        ctx.Whitelist.Autorizar(ComercioId, Numero);
        ctx.Parser.Responder(ResultadoParseo.Ok(Comando(AccionIntencion.CONSULTAR_STOCK, 0.95m, "FANTA", "600ML")));

        var respuesta = await ctx.Servicio.ProcesarMensajeAsync(ComercioId, Numero, "cuanto stock hay de fanta 600ml");

        Assert.Equal(RespuestasBot.NoEncontrado("No encontré 'FANTA' en el catálogo."), respuesta);
        var intencion = ctx.Intenciones.Intenciones.Single();
        Assert.Equal(EstadoIntencion.RECHAZADA, intencion.Estado);
    }

    [Fact]
    public async Task PresentacionAmbiguo_PideElegirPresentacion()
    {
        var ctx = new Contexto();
        ctx.Whitelist.Autorizar(ComercioId, Numero);
        var producto = Producto.Crear(ComercioId, null, "Coca Cola");
        producto.AgregarPresentacion("1.5L", 1500);
        producto.AgregarPresentacion("600ML", 900);
        ctx.Productos.Seed(producto);
        ctx.Parser.Responder(ResultadoParseo.Ok(Comando(AccionIntencion.CONSULTAR_STOCK, 0.95m, "COCA COLA")));

        var respuesta = await ctx.Servicio.ProcesarMensajeAsync(ComercioId, Numero, "cuanto stock hay de coca cola");

        Assert.Equal(RespuestasBot.ElegiPresentacion("COCA COLA", ["1.5L", "600ML"]), respuesta);
        var intencion = ctx.Intenciones.Intenciones.Single();
        Assert.Equal(EstadoIntencion.ACLARACION, intencion.Estado);
    }

    [Fact]
    public async Task ConsultarStock_EjecutaYResponde()
    {
        var ctx = new Contexto();
        ctx.Whitelist.Autorizar(ComercioId, Numero);
        var presentacion = SeedProducto(ctx, "Coca Cola", "1.5L");
        ctx.Ledger.Seed(MovimientoStock.EntradaManual(presentacion.Id, 12, null, Canal.WEB));
        ctx.Parser.Responder(ResultadoParseo.Ok(Comando(AccionIntencion.CONSULTAR_STOCK, 0.95m, "COCA COLA", "1.5L")));

        var respuesta = await ctx.Servicio.ProcesarMensajeAsync(ComercioId, Numero, "cuanto stock hay de coca cola 1.5l");

        Assert.Equal(RespuestasBot.StockConsultado("Coca Cola", "1.5L", 12, null), respuesta);
        var intencion = ctx.Intenciones.Intenciones.Single();
        Assert.Equal(EstadoIntencion.EJECUTADA, intencion.Estado);
        Assert.Contains(ctx.Auditoria.Eventos, e => e.Tipo == AuditoriaTipos.IntencionEjecutada);
    }

    [Fact]
    public async Task AccionDestructiva_PideConfirmacion_NoEjecuta()
    {
        var ctx = new Contexto();
        ctx.Whitelist.Autorizar(ComercioId, Numero);
        var presentacion = SeedProducto(ctx, "Coca Cola", "1.5L");
        ctx.Ledger.Seed(MovimientoStock.EntradaManual(presentacion.Id, 5, null, Canal.WEB));
        ctx.Parser.Responder(ResultadoParseo.Ok(Comando(AccionIntencion.ELIMINAR_PRODUCTO, 0.95m, "COCA COLA", "1.5L")));

        var respuesta = await ctx.Servicio.ProcesarMensajeAsync(ComercioId, Numero, "eliminar coca cola 1.5l");

        Assert.Equal(RespuestasBot.ConfirmarEliminar("Coca Cola", "1.5L", 5), respuesta);
        Assert.True(presentacion.Activa);
        var intencion = ctx.Intenciones.Intenciones.Single();
        Assert.Equal(EstadoIntencion.ESPERANDO_CONFIRMACION, intencion.Estado);
    }

    [Fact]
    public async Task ConfirmacionSi_EjecutaLaAccionPendiente()
    {
        var ctx = new Contexto();
        ctx.Whitelist.Autorizar(ComercioId, Numero);
        var presentacion = SeedProducto(ctx, "Coca Cola", "1.5L");
        ctx.Ledger.Seed(MovimientoStock.EntradaManual(presentacion.Id, 5, null, Canal.WEB));
        var pendiente = SeedConfirmacionPendiente(ctx, AccionIntencion.ELIMINAR_PRODUCTO, "COCA COLA", "1.5L");

        var respuesta = await ctx.Servicio.ProcesarMensajeAsync(ComercioId, Numero, "SI");

        Assert.Equal(RespuestasBot.ProductoEliminado("Coca Cola", "1.5L"), respuesta);
        Assert.False(presentacion.Activa);
        Assert.Equal(EstadoIntencion.EJECUTADA, pendiente.Estado);
    }

    [Fact]
    public async Task ConfirmacionNo_CancelaLaOperacion()
    {
        var ctx = new Contexto();
        ctx.Whitelist.Autorizar(ComercioId, Numero);
        var presentacion = SeedProducto(ctx, "Coca Cola", "1.5L");
        ctx.Ledger.Seed(MovimientoStock.EntradaManual(presentacion.Id, 5, null, Canal.WEB));
        var pendiente = SeedConfirmacionPendiente(ctx, AccionIntencion.ELIMINAR_PRODUCTO, "COCA COLA", "1.5L");

        var respuesta = await ctx.Servicio.ProcesarMensajeAsync(ComercioId, Numero, "NO");

        Assert.Equal(RespuestasBot.OperacionCancelada, respuesta);
        Assert.True(presentacion.Activa);
        Assert.Equal(EstadoIntencion.CANCELADA, pendiente.Estado);
    }

    [Fact]
    public async Task ConfirmacionExpirada_CancelaYRespondeTimeout()
    {
        var ctx = new Contexto();
        ctx.Whitelist.Autorizar(ComercioId, Numero);
        var comando = Comando(AccionIntencion.ELIMINAR_PRODUCTO, 0.95m, "COCA COLA", "1.5L");
        var pendiente = Intencion.Recibir(ComercioId, Numero, "mensaje original");
        pendiente.MarcarParseada(JsonSerializer.Serialize(comando));
        pendiente.PedirConfirmacion(DateTime.UtcNow.AddMinutes(-1));
        ctx.Intenciones.Seed(pendiente);

        var respuesta = await ctx.Servicio.ProcesarMensajeAsync(ComercioId, Numero, "SI");

        Assert.Equal(RespuestasBot.TimeoutExpirado, respuesta);
        Assert.Equal(EstadoIntencion.CANCELADA, pendiente.Estado);
    }

    [Fact]
    public async Task MensajeQueReemplazaConfirmacion_CancelaLaPendiente()
    {
        var ctx = new Contexto();
        ctx.Whitelist.Autorizar(ComercioId, Numero);
        var presentacion = SeedProducto(ctx, "Coca Cola", "1.5L");
        ctx.Ledger.Seed(MovimientoStock.EntradaManual(presentacion.Id, 5, null, Canal.WEB));
        var pendiente = SeedConfirmacionPendiente(ctx, AccionIntencion.ELIMINAR_PRODUCTO, "COCA COLA", "1.5L");
        ctx.Parser.Responder(ResultadoParseo.Ok(Comando(AccionIntencion.CONSULTAR_STOCK, 0.95m, "COCA COLA", "1.5L")));

        var respuesta = await ctx.Servicio.ProcesarMensajeAsync(ComercioId, Numero, "cuanto stock hay de coca cola 1.5l");

        Assert.Equal(EstadoIntencion.CANCELADA, pendiente.Estado);
        Assert.Equal(RespuestasBot.StockConsultado("Coca Cola", "1.5L", 5, null), respuesta);
    }

    [Fact]
    public async Task ConfirmacionDeIntencionDeOtroComercio_NoSeEjecuta()
    {
        var ctx = new Contexto();
        var otroComercio = Guid.NewGuid();
        ctx.Whitelist.Autorizar(ComercioId, Numero);
        ctx.Whitelist.Autorizar(otroComercio, Numero);
        var presentacion = SeedProducto(ctx, "Coca Cola", "1.5L");
        ctx.Ledger.Seed(MovimientoStock.EntradaManual(presentacion.Id, 5, null, Canal.WEB));

        var comando = Comando(AccionIntencion.ELIMINAR_PRODUCTO, 0.95m, "COCA COLA", "1.5L");
        var pendienteOtroComercio = Intencion.Recibir(otroComercio, Numero, "mensaje original");
        pendienteOtroComercio.MarcarParseada(JsonSerializer.Serialize(comando));
        pendienteOtroComercio.PedirConfirmacion(DateTime.UtcNow.AddMinutes(5));
        ctx.Intenciones.Seed(pendienteOtroComercio);

        var respuesta = await ctx.Servicio.ProcesarMensajeAsync(ComercioId, Numero, "SI");

        Assert.Equal(EstadoIntencion.ESPERANDO_CONFIRMACION, pendienteOtroComercio.Estado);
        Assert.True(presentacion.Activa);
        Assert.NotEqual(RespuestasBot.ProductoEliminado("Coca Cola", "1.5L"), respuesta);
    }
}
