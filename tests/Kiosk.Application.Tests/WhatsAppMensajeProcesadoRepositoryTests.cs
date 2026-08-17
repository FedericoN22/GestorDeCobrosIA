using Kiosk.Domain.Comercios;
using Kiosk.Infrastructure.Persistence;
using Kiosk.Infrastructure.Persistence.Repositorios;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Application.Tests;

public class WhatsAppMensajeProcesadoRepositoryTests : IDisposable
{
    private readonly SqliteConnection _conexion;

    public WhatsAppMensajeProcesadoRepositoryTests()
    {
        _conexion = new SqliteConnection("DataSource=:memory:");
        _conexion.Open();

        using var db = CrearContexto();
        db.Database.EnsureCreated();
        var comercio = Comercio.Crear("Comercio Test");
        db.Comercios.Add(comercio);
        db.SaveChanges();
        ComercioId = comercio.Id;
    }

    private Guid ComercioId { get; }

    public void Dispose()
    {
        _conexion.Dispose();
    }

    private KioskSqliteDbContext CrearContexto()
        => new(new DbContextOptionsBuilder<KioskSqliteDbContext>()
            .UseSqlite(_conexion)
            .Options);

    [Fact]
    public async Task MismoMessageId_DosVeces_LaSegundaSeRechaza()
    {
        var repo1 = new WhatsAppMensajeProcesadoRepository(CrearContexto());

        Assert.True(await repo1.IntentarRegistrarAsync(ComercioId, "msg-1"));

        var repo2 = new WhatsAppMensajeProcesadoRepository(CrearContexto());
        Assert.False(await repo2.IntentarRegistrarAsync(ComercioId, "msg-1"));
    }

    [Fact]
    public async Task MessageIdsDistintos_SeRegistranAmbos()
    {
        var repo = new WhatsAppMensajeProcesadoRepository(CrearContexto());

        Assert.True(await repo.IntentarRegistrarAsync(ComercioId, "msg-1"));
        Assert.True(await repo.IntentarRegistrarAsync(ComercioId, "msg-2"));
    }

    [Fact]
    public async Task NuevoDbContext_NoReprocesaMensajeYaRegistrado()
    {
        var repo1 = new WhatsAppMensajeProcesadoRepository(CrearContexto());
        Assert.True(await repo1.IntentarRegistrarAsync(ComercioId, "msg-1"));

        var repo2 = new WhatsAppMensajeProcesadoRepository(CrearContexto());
        Assert.False(await repo2.IntentarRegistrarAsync(ComercioId, "msg-1"));
    }

    [Fact]
    public async Task RequestsConcurrentes_MismoMessageId_SoloUnoRegistra()
    {
        var repo1 = new WhatsAppMensajeProcesadoRepository(CrearContexto());
        var repo2 = new WhatsAppMensajeProcesadoRepository(CrearContexto());

        var t1 = repo1.IntentarRegistrarAsync(ComercioId, "msg-concurrente");
        var t2 = repo2.IntentarRegistrarAsync(ComercioId, "msg-concurrente");
        var resultados = await Task.WhenAll(t1, t2);

        Assert.Equal(1, resultados.Count(r => r));
    }

    [Fact]
    public async Task MismoMessageId_EnOtroComercio_SiSeRegistra()
    {
        var repo = new WhatsAppMensajeProcesadoRepository(CrearContexto());
        Assert.True(await repo.IntentarRegistrarAsync(ComercioId, "msg-1"));

        var otroComercio = Comercio.Crear("Otro Comercio");
        using (var db = CrearContexto())
        {
            db.Comercios.Add(otroComercio);
            await db.SaveChangesAsync();
        }

        Assert.True(await repo.IntentarRegistrarAsync(otroComercio.Id, "msg-1"));
    }
}
