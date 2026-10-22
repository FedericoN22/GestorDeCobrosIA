using System.Text.Json;
using Kiosk.Application.CasosUso.Catalogos;
using Kiosk.Application.CasosUso.Chat;
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

public class PipelineConversacionalTests
{
    private static readonly Guid ComercioId = Guid.NewGuid();
    private const string CanalUsuarioId = "5491100000000";

    private sealed class Contexto
    {
        public Contexto()
        {
            Uow = new FakeUnitOfWork(() => Ledger.Commit());

            var stock = new ServicioStock(Productos, Ledger, Auditoria, Uow);
            var productos = new ServicioProductos(Productos, Categorias, Auditoria, Uow);
            Resolvedor = new ResolvedorCatalogos(Productos);
            Ejecutor = new EjecutorAcciones(stock, productos, Resolvedor, Ledger);
            Verificador = new VerificadorEfectoAplicado(Resolvedor);

            Pipeline = new PipelineConversacional(
                Intenciones, Uow, Config, Auditoria, Parser, Resolvedor, Ejecutor, Verificador, Ledger);
        }

        public FakeIntencionRepository Intenciones { get; } = new();
        public FakeUnitOfWork Uow { get; }
        public FakeConfiguracionRepository Config { get; } = new();
        public FakeAuditoriaRepository Auditoria { get; } = new();
        public ParserConExcepcion Parser { get; } = new();
        public FakeProductRepository Productos { get; } = new();
        public FakeCategoriaRepository Categorias { get; } = new();
        public FakeStockLedger Ledger { get; } = new();
        public ResolvedorCatalogos Resolvedor { get; }
        public EjecutorAcciones Ejecutor { get; }
        public VerificadorEfectoAplicado Verificador { get; }
        public PipelineConversacional Pipeline { get; }
    }

    private sealed class ParserConExcepcion : Kiosk.Application.Puertos.Integraciones.IIaParser
    {
        private readonly Queue<ResultadoParseo> _respuestas = new();
        private readonly List<string> _llamadas = [];
        public IReadOnlyList<string> Llamadas => _llamadas;

        public string? ExcepcionEnLaProximaLlamada { get; set; }

        public void Responder(ResultadoParseo resultado) => _respuestas.Enqueue(resultado);

        public Task<ResultadoParseo> ParsearAsync(string textoNormalizado, CancellationToken cancellationToken = default)
        {
            _llamadas.Add(textoNormalizado);

            if (ExcepcionEnLaProximaLlamada is not null)
            {
                var mensaje = ExcepcionEnLaProximaLlamada;
                ExcepcionEnLaProximaLlamada = null;
                throw new InvalidOperationException(mensaje);
            }

            return Task.FromResult(_respuestas.Count > 0
                ? _respuestas.Dequeue()
                : ResultadoParseo.Fallo("Sin respuesta configurada en el parser de tests."));
        }
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

    private static Intencion SeedConfirmacionPendiente(
        Contexto ctx,
        AccionIntencion accion,
        string producto,
        string presentacion,
        DateTime? expiraEn = null)
    {
        var comando = Comando(accion, 0.95m, producto, presentacion);
        var pendiente = Intencion.Recibir(ComercioId, Canal.WHATSAPP, CanalUsuarioId, "mensaje original");
        pendiente.MarcarParseada(JsonSerializer.Serialize(comando));
        pendiente.PedirConfirmacion(expiraEn ?? DateTime.UtcNow.AddMinutes(5));
        ctx.Intenciones.Seed(pendiente);
        return pendiente;
    }

    private static Intencion SeedIntencionEjecutando(
        Contexto ctx,
        AccionIntencion accion,
        string producto,
        string presentacion,
        int? precio = null)
    {
        var comando = Comando(accion, 0.95m, producto, presentacion, null, precio);
        var intencion = Intencion.Recibir(ComercioId, Canal.WHATSAPP, CanalUsuarioId, "mensaje original");
        intencion.MarcarParseada(JsonSerializer.Serialize(comando));
        intencion.PedirConfirmacion(DateTime.UtcNow.AddMinutes(5));
        intencion.MarcarEjecutando();
        ctx.Intenciones.Seed(intencion);
        return intencion;
    }

    [Fact]
    public async Task Saludo_RespondeBienvenida_NoLlamaAlParser()
    {
        var ctx = new Contexto();
        ctx.Config.Set(ComercioId, ClavesConfiguracion.BotBienvenida, "Bienvenido!");
        ctx.Config.Set(ComercioId, ClavesConfiguracion.BotNombre, "KioscoBot");

        var respuesta = await ctx.Pipeline.ProcesarAsync(ComercioId, Canal.WHATSAPP, CanalUsuarioId, "hola", false, CancellationToken.None);

        Assert.Equal("Bienvenido!", respuesta);
        Assert.Empty(ctx.Parser.Llamadas);
        Assert.Empty(ctx.Intenciones.Intenciones);
    }

    [Fact]
    public async Task Saludo_SinBienvenidaConfigurada_RespondeAyuda()
    {
        var ctx = new Contexto();
        ctx.Config.Set(ComercioId, ClavesConfiguracion.BotNombre, "KioscoBot");

        var respuesta = await ctx.Pipeline.ProcesarAsync(ComercioId, Canal.WHATSAPP, CanalUsuarioId, "ayuda", false, CancellationToken.None);

        Assert.Equal(RespuestasBot.Ayuda("KioscoBot"), respuesta);
        Assert.Empty(ctx.Intenciones.Intenciones);
    }

    [Fact]
    public async Task MensajeVacio_RespondeNoInterpretado()
    {
        var ctx = new Contexto();
        ctx.Config.Set(ComercioId, ClavesConfiguracion.BotNombre, "asistente-bot");

        var respuesta = await ctx.Pipeline.ProcesarAsync(ComercioId, Canal.WHATSAPP, CanalUsuarioId, "   ", false, CancellationToken.None);

        Assert.Equal(RespuestasBot.NoInterpretado("asistente-bot"), respuesta);
        Assert.Empty(ctx.Intenciones.Intenciones);
        Assert.Empty(ctx.Parser.Llamadas);
    }

    [Fact]
    public async Task ParserFalla_RechazaYRespondeNoInterpretado()
    {
        var ctx = new Contexto();
        ctx.Config.Set(ComercioId, ClavesConfiguracion.BotNombre, "asistente-bot");
        ctx.Parser.Responder(ResultadoParseo.Fallo("no entendí"));

        var respuesta = await ctx.Pipeline.ProcesarAsync(ComercioId, Canal.WHATSAPP, CanalUsuarioId, "asdfgh", false, CancellationToken.None);

        Assert.Equal(RespuestasBot.NoInterpretado("asistente-bot"), respuesta);
        var intencion = Assert.Single(ctx.Intenciones.Intenciones);
        Assert.Equal(EstadoIntencion.RECHAZADA, intencion.Estado);
        Assert.Contains(ctx.Auditoria.Eventos, e => e.Tipo == AuditoriaTipos.IntencionRechazada);
    }

    [Fact]
    public async Task MultiComando_RechazaLaIntencion()
    {
        var ctx = new Contexto();
        ctx.Parser.Responder(ResultadoParseo.MultiComando("más de una instrucción"));

        var respuesta = await ctx.Pipeline.ProcesarAsync(ComercioId, Canal.WHATSAPP, CanalUsuarioId, "agregar x y eliminar y", false, CancellationToken.None);

        Assert.Equal(RespuestasBot.MultiComando, respuesta);
        var intencion = Assert.Single(ctx.Intenciones.Intenciones);
        Assert.Equal(EstadoIntencion.RECHAZADA, intencion.Estado);
        Assert.Contains(ctx.Auditoria.Eventos, e => e.Tipo == AuditoriaTipos.IntencionRechazada);
    }

    [Fact]
    public async Task ConfianzaBaja_PideAclaracion_NoEjecuta()
    {
        var ctx = new Contexto();
        ctx.Config.Set(ComercioId, ClavesConfiguracion.BotNombre, "asistente");
        ctx.Parser.Responder(ResultadoParseo.Ok(Comando(AccionIntencion.CONSULTAR_STOCK, 0.4m, "COCA")));

        var respuesta = await ctx.Pipeline.ProcesarAsync(ComercioId, Canal.WHATSAPP, CanalUsuarioId, "chisme raro", false, CancellationToken.None);

        Assert.Equal(RespuestasBot.ConfianzaBaja("asistente"), respuesta);
        var intencion = Assert.Single(ctx.Intenciones.Intenciones);
        Assert.Equal(EstadoIntencion.ACLARACION, intencion.Estado);
        Assert.Contains(ctx.Auditoria.Eventos, e => e.Tipo == AuditoriaTipos.IntencionAclaracion);
    }

    [Fact]
    public async Task FaltanCampos_PideAclaracion()
    {
        var ctx = new Contexto();
        ctx.Config.Set(ComercioId, ClavesConfiguracion.BotNombre, "asistente");
        ctx.Parser.Responder(ResultadoParseo.Ok(Comando(
            AccionIntencion.AGREGAR_STOCK, 0.9m, "COCA", "1.5L", null, null, ["cantidad"])));

        var respuesta = await ctx.Pipeline.ProcesarAsync(ComercioId, Canal.WHATSAPP, CanalUsuarioId, "agregar coca", false, CancellationToken.None);

        Assert.Equal(RespuestasBot.FaltanCampos(["cantidad"], []), respuesta);
        var intencion = Assert.Single(ctx.Intenciones.Intenciones);
        Assert.Equal(EstadoIntencion.ACLARACION, intencion.Estado);
        Assert.Contains(ctx.Auditoria.Eventos, e => e.Tipo == AuditoriaTipos.IntencionAclaracion);
    }

    [Fact]
    public async Task ProductoNoEncontrado_RechazaYResponde()
    {
        var ctx = new Contexto();
        ctx.Parser.Responder(ResultadoParseo.Ok(Comando(AccionIntencion.CONSULTAR_STOCK, 0.95m, "FANTA", "600ML")));

        var respuesta = await ctx.Pipeline.ProcesarAsync(ComercioId, Canal.WHATSAPP, CanalUsuarioId, "cuanto stock hay de fanta 600ml", false, CancellationToken.None);

        Assert.Equal(RespuestasBot.NoEncontrado("No encontré 'FANTA' en el catálogo."), respuesta);
        var intencion = Assert.Single(ctx.Intenciones.Intenciones);
        Assert.Equal(EstadoIntencion.RECHAZADA, intencion.Estado);
        Assert.Contains(ctx.Auditoria.Eventos, e => e.Tipo == AuditoriaTipos.IntencionRechazada);
    }

    [Fact]
    public async Task PresentacionAmbiguo_PideElegirPresentacion()
    {
        var ctx = new Contexto();
        var producto = Producto.Crear(ComercioId, null, "Coca Cola");
        producto.AgregarPresentacion("1.5L", 1500);
        producto.AgregarPresentacion("600ML", 900);
        ctx.Productos.Seed(producto);
        ctx.Parser.Responder(ResultadoParseo.Ok(Comando(AccionIntencion.CONSULTAR_STOCK, 0.95m, "COCA COLA")));

        var respuesta = await ctx.Pipeline.ProcesarAsync(ComercioId, Canal.WHATSAPP, CanalUsuarioId, "cuanto stock hay de coca cola", false, CancellationToken.None);

        Assert.Equal(RespuestasBot.ElegiPresentacion("COCA COLA", ["1.5L", "600ML"]), respuesta);
        var intencion = Assert.Single(ctx.Intenciones.Intenciones);
        Assert.Equal(EstadoIntencion.ACLARACION, intencion.Estado);
        Assert.Contains(ctx.Auditoria.Eventos, e => e.Tipo == AuditoriaTipos.IntencionAclaracion);
    }

    [Fact]
    public async Task AccionDestructiva_PideConfirmacion_NoEjecuta()
    {
        var ctx = new Contexto();
        var presentacion = SeedProducto(ctx, "Coca Cola", "1.5L");
        ctx.Ledger.Seed(MovimientoStock.EntradaManual(presentacion.Id, 5, null, Canal.WEB));
        ctx.Parser.Responder(ResultadoParseo.Ok(Comando(AccionIntencion.ELIMINAR_PRODUCTO, 0.95m, "COCA COLA", "1.5L")));

        var respuesta = await ctx.Pipeline.ProcesarAsync(ComercioId, Canal.WHATSAPP, CanalUsuarioId, "eliminar coca cola 1.5l", false, CancellationToken.None);

        Assert.Equal(RespuestasBot.ConfirmarEliminar("Coca Cola", "1.5L", 5), respuesta);
        Assert.True(presentacion.Activa);
        var intencion = Assert.Single(ctx.Intenciones.Intenciones);
        Assert.Equal(EstadoIntencion.ESPERANDO_CONFIRMACION, intencion.Estado);
        Assert.Contains(ctx.Auditoria.Eventos, e => e.Tipo == AuditoriaTipos.IntencionConfirmacion);
    }

    [Fact]
    public async Task ConfirmacionSi_EjecutaLaAccionPendiente()
    {
        var ctx = new Contexto();
        var presentacion = SeedProducto(ctx, "Coca Cola", "1.5L");
        ctx.Ledger.Seed(MovimientoStock.EntradaManual(presentacion.Id, 5, null, Canal.WEB));
        var pendiente = SeedConfirmacionPendiente(ctx, AccionIntencion.ELIMINAR_PRODUCTO, "COCA COLA", "1.5L");

        var respuesta = await ctx.Pipeline.ProcesarAsync(ComercioId, Canal.WHATSAPP, CanalUsuarioId, "SI", false, CancellationToken.None);

        Assert.Equal(RespuestasBot.ProductoEliminado("Coca Cola", "1.5L"), respuesta);
        Assert.False(presentacion.Activa);
        Assert.Equal(EstadoIntencion.EJECUTADA, pendiente.Estado);
        Assert.Contains(ctx.Auditoria.Eventos, e => e.Tipo == AuditoriaTipos.IntencionConfirmacion);
        Assert.Contains(ctx.Auditoria.Eventos, e => e.Tipo == AuditoriaTipos.IntencionEjecutada);
    }

    [Fact]
    public async Task ConfirmacionNo_CancelaLaOperacion()
    {
        var ctx = new Contexto();
        var presentacion = SeedProducto(ctx, "Coca Cola", "1.5L");
        ctx.Ledger.Seed(MovimientoStock.EntradaManual(presentacion.Id, 5, null, Canal.WEB));
        var pendiente = SeedConfirmacionPendiente(ctx, AccionIntencion.ELIMINAR_PRODUCTO, "COCA COLA", "1.5L");

        var respuesta = await ctx.Pipeline.ProcesarAsync(ComercioId, Canal.WHATSAPP, CanalUsuarioId, "NO", false, CancellationToken.None);

        Assert.Equal(RespuestasBot.OperacionCancelada, respuesta);
        Assert.True(presentacion.Activa);
        Assert.Equal(EstadoIntencion.CANCELADA, pendiente.Estado);
        Assert.Contains(ctx.Auditoria.Eventos, e => e.Tipo == AuditoriaTipos.IntencionCancelada);
    }

    [Fact]
    public async Task ConfirmacionExpirada_CancelaYRespondeTimeout()
    {
        var ctx = new Contexto();
        SeedProducto(ctx, "Coca Cola", "1.5L");
        var pendiente = SeedConfirmacionPendiente(
            ctx, AccionIntencion.ELIMINAR_PRODUCTO, "COCA COLA", "1.5L", DateTime.UtcNow.AddMinutes(-1));

        var respuesta = await ctx.Pipeline.ProcesarAsync(ComercioId, Canal.WHATSAPP, CanalUsuarioId, "SI", false, CancellationToken.None);

        Assert.Equal(RespuestasBot.TimeoutExpirado, respuesta);
        Assert.Equal(EstadoIntencion.CANCELADA, pendiente.Estado);
    }

    [Fact]
    public async Task MensajeQueReemplazaAclaracion_CancelaLaPendiente()
    {
        var ctx = new Contexto();
        var presentacion = SeedProducto(ctx, "Coca Cola", "1.5L");
        ctx.Ledger.Seed(MovimientoStock.EntradaManual(presentacion.Id, 5, null, Canal.WEB));

        var comandoAclaracion = Comando(AccionIntencion.ELIMINAR_PRODUCTO, 0.95m, "COCA COLA", "1.5L");
        var pendiente = Intencion.Recibir(ComercioId, Canal.WHATSAPP, CanalUsuarioId, "eliminar coca cola 1.5l");
        pendiente.MarcarParseada(JsonSerializer.Serialize(comandoAclaracion));
        pendiente.PedirAclaracion("Falta confirmacion previa");
        ctx.Intenciones.Seed(pendiente);

        ctx.Parser.Responder(ResultadoParseo.Ok(Comando(AccionIntencion.CONSULTAR_STOCK, 0.95m, "COCA COLA", "1.5L")));

        var respuesta = await ctx.Pipeline.ProcesarAsync(ComercioId, Canal.WHATSAPP, CanalUsuarioId, "cuanto stock hay de coca cola 1.5l", false, CancellationToken.None);

        Assert.Equal(EstadoIntencion.CANCELADA, pendiente.Estado);
        Assert.Equal(RespuestasBot.StockConsultado("Coca Cola", "1.5L", 5, null), respuesta);
    }

    [Fact]
    public async Task AccionEjecutable_EjecutaYResponde()
    {
        var ctx = new Contexto();
        var presentacion = SeedProducto(ctx, "Coca Cola", "1.5L");
        ctx.Ledger.Seed(MovimientoStock.EntradaManual(presentacion.Id, 12, null, Canal.WEB));
        ctx.Parser.Responder(ResultadoParseo.Ok(Comando(AccionIntencion.CONSULTAR_STOCK, 0.95m, "COCA COLA", "1.5L")));

        var respuesta = await ctx.Pipeline.ProcesarAsync(ComercioId, Canal.WHATSAPP, CanalUsuarioId, "cuanto stock hay de coca cola 1.5l", false, CancellationToken.None);

        Assert.Equal(RespuestasBot.StockConsultado("Coca Cola", "1.5L", 12, null), respuesta);
        var intencion = Assert.Single(ctx.Intenciones.Intenciones);
        Assert.Equal(EstadoIntencion.EJECUTADA, intencion.Estado);
        Assert.Contains(ctx.Auditoria.Eventos, e => e.Tipo == AuditoriaTipos.IntencionEjecutada);
    }

    [Fact]
    public async Task IntencionEjecutandoReciente_NoSeReconciliaYRespondeQueEstaEnProceso()
    {
        var ctx = new Contexto();
        var presentacion = SeedProducto(ctx, "Coca Cola", "1.5L");
        var ejecutando = SeedIntencionEjecutando(ctx, AccionIntencion.ELIMINAR_PRODUCTO, "COCA COLA", "1.5L");

        var respuesta = await ctx.Pipeline.ProcesarAsync(ComercioId, Canal.WHATSAPP, CanalUsuarioId, "SI", false, CancellationToken.None);

        Assert.Equal(RespuestasBot.OperacionEnProceso, respuesta);
        Assert.Equal(EstadoIntencion.EJECUTANDO, ejecutando.Estado);
        Assert.True(presentacion.Activa);
        Assert.Empty(ctx.Parser.Llamadas);
    }

    [Fact]
    public async Task IntencionEjecutandoVencida_ConEfectoYaAplicado_ReconciliaAExecutada()
    {
        var ctx = new Contexto();
        ctx.Config.Set(ComercioId, ClavesConfiguracion.BotTimeoutEjecucionSegundos, "0");
        var presentacion = SeedProducto(ctx, "Coca Cola", "1.5L");
        var ejecutando = SeedIntencionEjecutando(ctx, AccionIntencion.ELIMINAR_PRODUCTO, "COCA COLA", "1.5L");
        presentacion.Desactivar();

        var respuesta = await ctx.Pipeline.ProcesarAsync(ComercioId, Canal.WHATSAPP, CanalUsuarioId, "SI", false, CancellationToken.None);

        Assert.Equal(RespuestasBot.OperacionReconciliadaAplicada, respuesta);
        Assert.Equal(EstadoIntencion.EJECUTADA, ejecutando.Estado);
        Assert.Contains(ctx.Auditoria.Eventos, e => e.Tipo == AuditoriaTipos.IntencionReconciliada);
        Assert.Empty(ctx.Parser.Llamadas);
    }

    [Fact]
    public async Task IntencionEjecutandoVencida_ConPrecioYaAplicado_ReconciliaAExecutada()
    {
        var ctx = new Contexto();
        ctx.Config.Set(ComercioId, ClavesConfiguracion.BotTimeoutEjecucionSegundos, "0");
        SeedProducto(ctx, "Coca Cola", "1.5L");
        var ejecutando = SeedIntencionEjecutando(ctx, AccionIntencion.MODIFICAR_PRECIO, "COCA COLA", "1.5L", precio: 15);

        var respuesta = await ctx.Pipeline.ProcesarAsync(ComercioId, Canal.WHATSAPP, CanalUsuarioId, "SI", false, CancellationToken.None);

        Assert.Equal(RespuestasBot.OperacionReconciliadaAplicada, respuesta);
        Assert.Equal(EstadoIntencion.EJECUTADA, ejecutando.Estado);
    }

    [Fact]
    public async Task IntencionEjecutandoVencida_SinEfectoVerificable_QuedaEnErrorYNoReEjecuta()
    {
        var ctx = new Contexto();
        ctx.Config.Set(ComercioId, ClavesConfiguracion.BotTimeoutEjecucionSegundos, "0");
        var presentacion = SeedProducto(ctx, "Coca Cola", "1.5L");
        var ejecutando = SeedIntencionEjecutando(ctx, AccionIntencion.ELIMINAR_PRODUCTO, "COCA COLA", "1.5L");

        var respuesta = await ctx.Pipeline.ProcesarAsync(ComercioId, Canal.WHATSAPP, CanalUsuarioId, "SI", false, CancellationToken.None);

        Assert.Equal(RespuestasBot.OperacionReconciliadaNoVerificable, respuesta);
        Assert.Equal(EstadoIntencion.ERROR, ejecutando.Estado);
        Assert.True(presentacion.Activa);
        Assert.Contains(ctx.Auditoria.Eventos, e => e.Tipo == AuditoriaTipos.IntencionReconciliada);
        Assert.Empty(ctx.Parser.Llamadas);
    }

    [Fact]
    public async Task IntencionEjecutandoVencida_NuncaVuelveAEsperandoConfirmacion()
    {
        var ctx = new Contexto();
        ctx.Config.Set(ComercioId, ClavesConfiguracion.BotTimeoutEjecucionSegundos, "0");
        SeedProducto(ctx, "Coca Cola", "1.5L");
        var ejecutando = SeedIntencionEjecutando(ctx, AccionIntencion.ELIMINAR_PRODUCTO, "COCA COLA", "1.5L");

        await ctx.Pipeline.ProcesarAsync(ComercioId, Canal.WHATSAPP, CanalUsuarioId, "SI", false, CancellationToken.None);

        Assert.NotEqual(EstadoIntencion.ESPERANDO_CONFIRMACION, ejecutando.Estado);
    }

    [Fact]
    public async Task FallaDePersistenciaTrasElClaim_LaIntencionQuedaEjecutandoYNoSeReEjecutaEnElMensajeSiguiente()
    {
        var ctx = new Contexto();
        SeedProducto(ctx, "Coca Cola", "1.5L");
        var pendiente = SeedConfirmacionPendiente(ctx, AccionIntencion.ELIMINAR_PRODUCTO, "COCA COLA", "1.5L");

        ctx.Uow.LanzarError = true;
        var primera = await ctx.Pipeline.ProcesarAsync(ComercioId, Canal.WHATSAPP, CanalUsuarioId, "SI", false, CancellationToken.None);

        Assert.Equal(RespuestasBot.Error, primera);
        Assert.Equal(EstadoIntencion.EJECUTANDO, pendiente.Estado);

        ctx.Uow.LanzarError = false;
        ctx.Config.Set(ComercioId, ClavesConfiguracion.BotTimeoutEjecucionSegundos, "0");

        var segunda = await ctx.Pipeline.ProcesarAsync(ComercioId, Canal.WHATSAPP, CanalUsuarioId, "SI", false, CancellationToken.None);

        Assert.NotEqual(EstadoIntencion.ESPERANDO_CONFIRMACION, pendiente.Estado);
        Assert.NotEqual(EstadoIntencion.EJECUTANDO, pendiente.Estado);
        Assert.Single(ctx.Auditoria.Eventos, e => e.Tipo == AuditoriaTipos.PresentacionDesactivada);
        Assert.Empty(ctx.Parser.Llamadas);
    }

    [Fact]
    public async Task ConfirmacionSi_AplicaElEfectoMientrasLaIntencionEstaEjecutando_YLaMarcaEjecutadaDespues()
    {
        var ctx = new Contexto();
        var presentacion = SeedProducto(ctx, "Coca Cola", "1.5L");
        ctx.Ledger.Seed(MovimientoStock.EntradaManual(presentacion.Id, 5, null, Canal.WEB));
        var pendiente = SeedConfirmacionPendiente(ctx, AccionIntencion.ELIMINAR_PRODUCTO, "COCA COLA", "1.5L");

        var estadosAlGuardar = new List<EstadoIntencion>();
        ctx.Uow.AlGuardar = () => estadosAlGuardar.Add(pendiente.Estado);

        await ctx.Pipeline.ProcesarAsync(ComercioId, Canal.WHATSAPP, CanalUsuarioId, "SI", false, CancellationToken.None);

        Assert.Equal([EstadoIntencion.EJECUTANDO, EstadoIntencion.EJECUTADA], estadosAlGuardar);
        Assert.False(presentacion.Activa);
    }

    [Fact]
    public async Task ExcepcionEnParser_MarcaIntencionComoErrorYRespondeError()
    {
        var ctx = new Contexto();
        SeedProducto(ctx, "Coca Cola", "1.5L");
        ctx.Parser.ExcepcionEnLaProximaLlamada = "Falla simulada del parser";

        var respuesta = await ctx.Pipeline.ProcesarAsync(ComercioId, Canal.WHATSAPP, CanalUsuarioId, "consultar stock", false, CancellationToken.None);

        Assert.Equal(RespuestasBot.Error, respuesta);
        var intencion = Assert.Single(ctx.Intenciones.Intenciones);
        Assert.Equal(EstadoIntencion.ERROR, intencion.Estado);
    }

    [Fact]
    public async Task TextoConEspaciosAlInicioYAlFinal_SeNormalizaAntesDeParsear()
    {
        var ctx = new Contexto();
        SeedProducto(ctx, "Coca Cola", "1.5L");
        ctx.Parser.Responder(ResultadoParseo.Ok(Comando(AccionIntencion.CONSULTAR_STOCK, 0.95m, "COCA COLA", "1.5L")));

        await ctx.Pipeline.ProcesarAsync(ComercioId, Canal.WHATSAPP, CanalUsuarioId, "   cuanto stock hay de coca cola 1.5l   ", false, CancellationToken.None);

        var llamada = Assert.Single(ctx.Parser.Llamadas);
        Assert.Equal("CUANTO STOCK HAY DE COCA COLA 1.5L", llamada);
    }

    [Fact]
    public async Task IntencionPendente_AisladaPorComercioCanalYUsuario()
    {
        var comercioA = Guid.NewGuid();
        var comercioB = Guid.NewGuid();
        var usuarioX = "usuario-x";

        var intencionDeAWhatsapp = Intencion.Recibir(comercioA, Canal.WHATSAPP, usuarioX, "mensaje para A whatsapp");
        intencionDeAWhatsapp.MarcarParseada(JsonSerializer.Serialize(Comando(AccionIntencion.ELIMINAR_PRODUCTO, 0.95m, "COCA", "1.5L")));
        intencionDeAWhatsapp.PedirConfirmacion(DateTime.UtcNow.AddMinutes(5));

        var intencionDeAWeb = Intencion.Recibir(comercioA, Canal.WEB, usuarioX, "mensaje para A web");
        intencionDeAWeb.MarcarParseada(JsonSerializer.Serialize(Comando(AccionIntencion.ELIMINAR_PRODUCTO, 0.95m, "COCA", "1.5L")));
        intencionDeAWeb.PedirConfirmacion(DateTime.UtcNow.AddMinutes(5));

        var intencionDeBWhatsapp = Intencion.Recibir(comercioB, Canal.WHATSAPP, usuarioX, "mensaje para B whatsapp");
        intencionDeBWhatsapp.MarcarParseada(JsonSerializer.Serialize(Comando(AccionIntencion.ELIMINAR_PRODUCTO, 0.95m, "COCA", "1.5L")));
        intencionDeBWhatsapp.PedirConfirmacion(DateTime.UtcNow.AddMinutes(5));

        using var db = new DbEnMemoria(intencionDeAWhatsapp, intencionDeAWeb, intencionDeBWhatsapp);
        var repo = new IntencionRepositoryEnMemoria(db);

        var pendienteAWhatsapp = await repo.GetPendienteAsync(comercioA, Canal.WHATSAPP, usuarioX);
        var pendienteAWeb = await repo.GetPendienteAsync(comercioA, Canal.WEB, usuarioX);
        var pendienteBWhatsapp = await repo.GetPendienteAsync(comercioB, Canal.WHATSAPP, usuarioX);

        Assert.NotNull(pendienteAWhatsapp);
        Assert.Equal(intencionDeAWhatsapp.Id, pendienteAWhatsapp!.Id);

        Assert.NotNull(pendienteAWeb);
        Assert.Equal(intencionDeAWeb.Id, pendienteAWeb!.Id);
        Assert.NotEqual(pendienteAWhatsapp.Id, pendienteAWeb.Id);

        Assert.NotNull(pendienteBWhatsapp);
        Assert.Equal(intencionDeBWhatsapp.Id, pendienteBWhatsapp!.Id);
        Assert.NotEqual(pendienteAWhatsapp.Id, pendienteBWhatsapp.Id);

        var pendienteBWeb = await repo.GetPendienteAsync(comercioB, Canal.WEB, usuarioX);
        Assert.Null(pendienteBWeb);

        var pendienteAWhatsappDeOtroUsuario = await repo.GetPendienteAsync(comercioA, Canal.WHATSAPP, "otro-usuario");
        Assert.Null(pendienteAWhatsappDeOtroUsuario);
    }

    private sealed class DbEnMemoria : IDisposable
    {
        public List<Intencion> Intenciones { get; }

        public DbEnMemoria(params Intencion[] intenciones)
        {
            Intenciones = intenciones.ToList();
        }

        public void Dispose() { }
    }

    private sealed class IntencionRepositoryEnMemoria : Kiosk.Application.Puertos.Repositorios.IIntencionRepository
    {
        private static readonly EstadoIntencion[] EstadosPendientes =
        {
            EstadoIntencion.RECIBIDA,
            EstadoIntencion.PARSEADA,
            EstadoIntencion.ACLARACION,
            EstadoIntencion.ESPERANDO_CONFIRMACION
        };

        private readonly DbEnMemoria _db;

        public IntencionRepositoryEnMemoria(DbEnMemoria db)
        {
            _db = db;
        }

        public Task<Kiosk.Domain.Whatsapp.Intencion?> GetPendienteAsync(
            Guid comercioId,
            Canal canal,
            string canalUsuarioId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<Kiosk.Domain.Whatsapp.Intencion?>(
                _db.Intenciones
                    .Where(i => i.ComercioId == comercioId
                                && i.Canal == canal
                                && i.CanalUsuarioId == canalUsuarioId
                                && EstadosPendientes.Contains(i.Estado))
                    .OrderByDescending(i => i.CreatedAt)
                    .FirstOrDefault());

        public Task<Kiosk.Domain.Whatsapp.Intencion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<Kiosk.Domain.Whatsapp.Intencion?>(_db.Intenciones.FirstOrDefault(i => i.Id == id));

        public Task<IReadOnlyList<Kiosk.Domain.Whatsapp.Intencion>> GetEjecutandoAsync(
            Guid comercioId,
            Canal canal,
            string canalUsuarioId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Kiosk.Domain.Whatsapp.Intencion>>(_db.Intenciones
                .Where(i => i.ComercioId == comercioId
                            && i.Canal == canal
                            && i.CanalUsuarioId == canalUsuarioId
                            && i.Estado == EstadoIntencion.EJECUTANDO)
                .OrderByDescending(i => i.UpdatedAt)
                .ToList());

        public Task<Kiosk.Application.Puertos.Repositorios.ResultadoReclamo> TryReclamarAsync(
            Guid id,
            Guid comercioId,
            Canal canal,
            string canalUsuarioId,
            CancellationToken cancellationToken = default)
        {
            var intencion = _db.Intenciones.FirstOrDefault(i => i.Id == id);

            if (intencion is null
                || intencion.ComercioId != comercioId
                || intencion.Canal != canal
                || intencion.CanalUsuarioId != canalUsuarioId)
            {
                return Task.FromResult(Kiosk.Application.Puertos.Repositorios.ResultadoReclamo.NoResuelta);
            }

            if (intencion.Estado == EstadoIntencion.ESPERANDO_CONFIRMACION)
            {
                intencion.MarcarEjecutando();
                return Task.FromResult(Kiosk.Application.Puertos.Repositorios.ResultadoReclamo.Reclamado);
            }

            return Task.FromResult(intencion.Estado switch
            {
                EstadoIntencion.EJECUTANDO => Kiosk.Application.Puertos.Repositorios.ResultadoReclamo.EnEjecucion,
                EstadoIntencion.EJECUTADA => Kiosk.Application.Puertos.Repositorios.ResultadoReclamo.YaAplicada,
                _ => Kiosk.Application.Puertos.Repositorios.ResultadoReclamo.NoResuelta
            });
        }

        public void Add(Kiosk.Domain.Whatsapp.Intencion intencion)
            => _db.Intenciones.Add(intencion);
    }
}

