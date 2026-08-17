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
        var intencion = Intencion.Recibir(comercioId, Numero, texto);
        intencion.MarcarParseada("{\"accion\":\"CONSULTAR_STOCK\"}");
        intencion.PedirConfirmacion(DateTime.UtcNow.AddMinutes(5));
        return intencion;
    }

    [Fact]
    public async Task MismoNumero_DistintoComercio_ComercioADevuelveSoloSuIntencion()
    {
        var intencionA = CrearPendiente(_comercioA, "mensaje del comercio A");
        var intencionB = CrearPendiente(_comercioB, "mensaje del comercio B");
        SeedIntenciones(intencionA, intencionB);

        var repo = new IntencionRepository(CrearContexto());
        var pendiente = await repo.GetPendienteAsync(_comercioA, Numero);

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
        var pendiente = await repo.GetPendienteAsync(_comercioB, Numero);

        Assert.NotNull(pendiente);
        Assert.Equal(intencionB.Id, pendiente.Id);
        Assert.Equal("mensaje del comercio B", pendiente.TextoOriginal);
        Assert.NotEqual(intencionA.Id, pendiente.Id);
    }

    [Fact]
    public async Task NumeroSinIntencionPendiente_DevuelveNull()
    {
        var intencionA = CrearPendiente(_comercioA, "mensaje del comercio A");
        SeedIntenciones(intencionA);

        var repo = new IntencionRepository(CrearContexto());
        var pendiente = await repo.GetPendienteAsync(_comercioA, "5491222222222");

        Assert.Null(pendiente);
    }

    [Fact]
    public async Task IntencionPendienteDeOtroComercio_NoEsVisibleParaEsteComercio()
    {
        var intencionA = CrearPendiente(_comercioA, "mensaje del comercio A");
        SeedIntenciones(intencionA);

        var repo = new IntencionRepository(CrearContexto());
        var pendiente = await repo.GetPendienteAsync(_comercioB, Numero);

        Assert.Null(pendiente);
    }
}
