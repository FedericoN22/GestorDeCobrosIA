using Kiosk.Application.CasosUso.Catalogos;
using Kiosk.Application.CasosUso.Chat;
using Kiosk.Application.CasosUso.Stock;
using Kiosk.Application.CasosUso.Whatsapp;
using Kiosk.Application.Puertos.Integraciones;
using Kiosk.Application.Tests.TestDoubles;
using Kiosk.Domain.Common;
using Kiosk.Domain.Whatsapp;

namespace Kiosk.Application.Tests;

public class ServicioChatTests
{
    private sealed class Contexto
    {
        public Contexto()
        {
            Uow = new FakeUnitOfWork(() => Ledger.Commit());

            var stock = new ServicioStock(Productos, Ledger, Auditoria, Uow);
            var productos = new ServicioProductos(Productos, Categorias, Auditoria, Uow);
            var resolvedor = new ResolvedorCatalogos(Productos);
            var ejecutor = new EjecutorAcciones(stock, productos, resolvedor, Ledger, Config);
            var verificador = new VerificadorEfectoAplicado(resolvedor);
            var confirmationPolicy = new ConfirmationPolicy(Config);

            Pipeline = new PipelineConversacional(
                Intenciones, Uow, Config, Auditoria, Parser, resolvedor, ejecutor, verificador, Ledger, confirmationPolicy);

            Servicio = new ServicioChat(Pipeline);
        }

        public FakeIntencionRepository Intenciones { get; } = new();
        public FakeUnitOfWork Uow { get; }
        public FakeConfiguracionRepository Config { get; } = new();
        public FakeAuditoriaRepository Auditoria { get; } = new();
        public ParserObservable Parser { get; } = new();
        public FakeProductRepository Productos { get; } = new();
        public FakeCategoriaRepository Categorias { get; } = new();
        public FakeStockLedger Ledger { get; } = new();
        public PipelineConversacional Pipeline { get; }
        public ServicioChat Servicio { get; }
    }

    private sealed class ParserObservable : IIaParser
    {
        public ResultadoParseo ProximaRespuesta { get; set; } = ResultadoParseo.Fallo("fin");

        public string? UltimoTextoRecibido { get; private set; }

        public Task<ResultadoParseo> ParsearAsync(string textoNormalizado, CancellationToken cancellationToken = default)
        {
            UltimoTextoRecibido = textoNormalizado;
            return Task.FromResult(ProximaRespuesta);
        }
    }

    [Fact]
    public async Task ProcesarAsync_CreaIntencionConCanalWeb_YCanalUsuarioIdEsUsuarioIdComoString()
    {
        var ctx = new Contexto();
        var comercioId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();

        await ctx.Servicio.ProcesarAsync(comercioId, usuarioId, "consultar stock", CancellationToken.None);

        var intencion = Assert.Single(ctx.Intenciones.Intenciones);
        Assert.Equal(Canal.WEB, intencion.Canal);
        Assert.Equal(usuarioId.ToString(), intencion.CanalUsuarioId);
        Assert.Equal(comercioId, intencion.ComercioId);
    }

    [Fact]
    public async Task ProcesarAsync_NoMarcaFueAudio()
    {
        var ctx = new Contexto();
        var comercioId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();

        await ctx.Servicio.ProcesarAsync(comercioId, usuarioId, "consultar stock", CancellationToken.None);

        var intencion = Assert.Single(ctx.Intenciones.Intenciones);
        Assert.False(intencion.FueAudio);
    }

    [Fact]
    public async Task ProcesarAsync_PropagaTextoOriginalALaIntencion()
    {
        var ctx = new Contexto();
        var comercioId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();

        await ctx.Servicio.ProcesarAsync(comercioId, usuarioId, "cuanto stock hay de coca", CancellationToken.None);

        var intencion = Assert.Single(ctx.Intenciones.Intenciones);
        Assert.Equal("cuanto stock hay de coca", intencion.TextoOriginal);
    }

    [Fact]
    public async Task ProcesarAsync_DevuelveRespuestaDeLaPipeline()
    {
        var ctx = new Contexto();
        var comercioId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        ctx.Config.Set(comercioId, ClavesConfiguracion.BotBienvenida, "Bienvenido Web!");

        var respuesta = await ctx.Servicio.ProcesarAsync(comercioId, usuarioId, "hola", CancellationToken.None);

        Assert.Equal("Bienvenido Web!", respuesta);
    }
}
