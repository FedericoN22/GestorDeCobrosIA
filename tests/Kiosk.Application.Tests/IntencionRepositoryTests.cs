using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Common;
using Kiosk.Domain.Comercios;
using Kiosk.Domain.Whatsapp;
using Kiosk.Infrastructure.Persistence;
using Kiosk.Infrastructure.Persistence.Repositorios;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Application.Tests;

public class IntencionRepositoryTests : IDisposable
{
    private const string Numero = "5491100000000";

    private readonly SqliteConnection _conexion;
    private readonly Guid _comercioA;
    private readonly Guid _comercioB;

    public IntencionRepositoryTests()
    {
        _conexion = new SqliteConnection("DataSource=:memory:");
        _conexion.Open();

        using var db = CrearContexto();
        db.Database.EnsureCreated();
        var comercioA = Comercio.Crear("Comercio A");
        var comercioB = Comercio.Crear("Comercio B");
        db.Comercios.AddRange(comercioA, comercioB);
        db.SaveChanges();
        _comercioA = comercioA.Id;
        _comercioB = comercioB.Id;
    }

    public void Dispose()
    {
        _conexion.Dispose();
    }

    private KioskSqliteDbContext CrearContexto()
        => new(new DbContextOptionsBuilder<KioskSqliteDbContext>()
            .UseSqlite(_conexion)
            .Options);

    private void SeedIntenciones(params Intencion[] intenciones)
    {
        using var db = CrearContexto();
        db.Intenciones.AddRange(intenciones);
        db.SaveChanges();
    }

    private static Intencion CrearPendiente(Guid comercioId, string texto)
    {
        var intencion = Intencion.Recibir(comercioId, Canal.WHATSAPP, Numero, texto);
        intencion.MarcarParseada("{\"accion\":\"CONSULTAR_STOCK\"}");
        intencion.PedirConfirmacion(DateTime.UtcNow.AddMinutes(5));
        return intencion;
    }

    private static Intencion CrearEjecutando(Guid comercioId, string texto)
    {
        var intencion = CrearPendiente(comercioId, texto);
        intencion.MarcarEjecutando();
        return intencion;
    }

    [Fact]
    public async Task MismoNumero_DistintoComercio_ComercioADevuelveSoloSuIntencion()
    {
        var intencionA = CrearPendiente(_comercioA, "mensaje del comercio A");
        var intencionB = CrearPendiente(_comercioB, "mensaje del comercio B");
        SeedIntenciones(intencionA, intencionB);

        var repo = new IntencionRepository(CrearContexto());
        var pendiente = await repo.GetPendienteAsync(_comercioA, Canal.WHATSAPP, Numero);

        Assert.NotNull(pendiente);
        Assert.Equal(intencionA.Id, pendiente.Id);
        Assert.Equal("mensaje del comercio A", pendiente.TextoOriginal);
        Assert.NotEqual(intencionB.Id, pendiente.Id);
    }

    [Fact]
    public async Task MismoNumero_DistintoComercio_ComercioBDevuelveSoloSuIntencion()
    {
        var intencionA = CrearPendiente(_comercioA, "mensaje del comercio A");
        var intencionB = CrearPendiente(_comercioB, "mensaje del comercio B");
        SeedIntenciones(intencionA, intencionB);

        var repo = new IntencionRepository(CrearContexto());
        var pendiente = await repo.GetPendienteAsync(_comercioB, Canal.WHATSAPP, Numero);
        Assert.NotNull(pendiente);
        Assert.Equal("mensaje del comercio B", pendiente.TextoOriginal);
        Assert.NotEqual(intencionA.Id, pendiente.Id);
    }

    [Fact]
    public async Task NumeroSinIntencionPendiente_DevuelveNull()
    {
        var intencionA = CrearPendiente(_comercioA, "mensaje del comercio A");
        SeedIntenciones(intencionA);

        var repo = new IntencionRepository(CrearContexto());
        var pendiente = await repo.GetPendienteAsync(_comercioA, Canal.WHATSAPP, "5491222222222");

        Assert.Null(pendiente);
    }

    [Fact]
    public async Task IntencionPendienteDeOtroComercio_NoEsVisibleParaEsteComercio()
    {
        var intencionA = CrearPendiente(_comercioA, "mensaje del comercio A");
        SeedIntenciones(intencionA);

        var repo = new IntencionRepository(CrearContexto());
        var pendiente = await repo.GetPendienteAsync(_comercioB, Canal.WHATSAPP, Numero);

        Assert.Null(pendiente);
    }

    [Fact]
    public async Task TryReclamarAsync_SobrePendiente_ReclamaYPasaAEjecutando()
    {
        var intencion = CrearPendiente(_comercioA, "mensaje del comercio A");
        SeedIntenciones(intencion);

        var repo = new IntencionRepository(CrearContexto());
        var resultado = await repo.TryReclamarAsync(intencion.Id, _comercioA, Canal.WHATSAPP, Numero);

        Assert.Equal(ResultadoReclamo.Reclamado, resultado);

        using var db = CrearContexto();
        var guardada = await db.Intenciones.FirstAsync(i => i.Id == intencion.Id);
        Assert.Equal(EstadoIntencion.EJECUTANDO, guardada.Estado);
    }

    [Fact]
    public async Task TryReclamarAsync_SobreYaReclamada_NoReclamaYDevuelveEnEjecucion()
    {
        var intencion = CrearEjecutando(_comercioA, "mensaje del comercio A");
        SeedIntenciones(intencion);

        var repo = new IntencionRepository(CrearContexto());
        var resultado = await repo.TryReclamarAsync(intencion.Id, _comercioA, Canal.WHATSAPP, Numero);

        Assert.Equal(ResultadoReclamo.EnEjecucion, resultado);
    }

    [Fact]
    public async Task TryReclamarAsync_SobreYaEjecutada_DevuelveYaAplicada()
    {
        var intencion = CrearPendiente(_comercioA, "mensaje del comercio A");
        intencion.Ejecutar("{\"respuesta\":\"listo\"}");
        SeedIntenciones(intencion);

        var repo = new IntencionRepository(CrearContexto());
        var resultado = await repo.TryReclamarAsync(intencion.Id, _comercioA, Canal.WHATSAPP, Numero);

        Assert.Equal(ResultadoReclamo.YaAplicada, resultado);
    }

    [Fact]
    public async Task TryReclamarAsync_DeOtroComercio_NoReclama()
    {
        var intencion = CrearPendiente(_comercioA, "mensaje del comercio A");
        SeedIntenciones(intencion);

        var repo = new IntencionRepository(CrearContexto());
        var resultado = await repo.TryReclamarAsync(intencion.Id, _comercioB, Canal.WHATSAPP, Numero);

        Assert.Equal(ResultadoReclamo.NoResuelta, resultado);
    }

    [Fact]
    public async Task TryReclamarAsync_DeOtroCanal_NoReclama()
    {
        var intencion = CrearPendiente(_comercioA, "mensaje del comercio A");
        SeedIntenciones(intencion);

        var repo = new IntencionRepository(CrearContexto());
        var resultado = await repo.TryReclamarAsync(intencion.Id, _comercioA, Canal.WEB, Numero);

        Assert.Equal(ResultadoReclamo.NoResuelta, resultado);
    }

    [Fact]
    public async Task TryReclamarAsync_DeOtroUsuario_NoReclama()
    {
        var intencion = CrearPendiente(_comercioA, "mensaje del comercio A");
        SeedIntenciones(intencion);

        var repo = new IntencionRepository(CrearContexto());
        var resultado = await repo.TryReclamarAsync(intencion.Id, _comercioA, Canal.WHATSAPP, "5491222222222");

        Assert.Equal(ResultadoReclamo.NoResuelta, resultado);
    }

    [Fact]
    public async Task DosClaimsSobreLaMismaIntencion_SoloUnoGana()
    {
        var intencion = CrearPendiente(_comercioA, "mensaje del comercio A");
        SeedIntenciones(intencion);

        var primerRepo = new IntencionRepository(CrearContexto());
        var segundoRepo = new IntencionRepository(CrearContexto());

        var primero = await primerRepo.TryReclamarAsync(intencion.Id, _comercioA, Canal.WHATSAPP, Numero);
        var segundo = await segundoRepo.TryReclamarAsync(intencion.Id, _comercioA, Canal.WHATSAPP, Numero);

        Assert.Equal(ResultadoReclamo.Reclamado, primero);
        Assert.Equal(ResultadoReclamo.EnEjecucion, segundo);
    }

    [Fact]
    public async Task GetEjecutandoAsync_DevuelveSoloLasIntencionesEjecutandoDelDueno()
    {
        var ejecutandoA = CrearEjecutando(_comercioA, "ejecutando del comercio A");
        var pendienteA = CrearPendiente(_comercioA, "pendiente del comercio A");
        var ejecutandoDeOtroComercio = CrearEjecutando(_comercioB, "ejecutando del comercio B");
        SeedIntenciones(ejecutandoA, pendienteA, ejecutandoDeOtroComercio);

        var repo = new IntencionRepository(CrearContexto());
        var encontradas = await repo.GetEjecutandoAsync(_comercioA, Canal.WHATSAPP, Numero);

        var unica = Assert.Single(encontradas);
        Assert.Equal(ejecutandoA.Id, unica.Id);
    }

    [Fact]
    public async Task GetEjecutandoAsync_NoDevuelveLasPendientes()
    {
        var pendiente = CrearPendiente(_comercioA, "pendiente del comercio A");
        SeedIntenciones(pendiente);

        var repo = new IntencionRepository(CrearContexto());
        var encontradas = await repo.GetEjecutandoAsync(_comercioA, Canal.WHATSAPP, Numero);

        Assert.Empty(encontradas);
    }
}
