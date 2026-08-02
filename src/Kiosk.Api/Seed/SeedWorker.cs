using Kiosk.Application.Puertos;
using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Comercios;
using Kiosk.Domain.Usuarios;
using Kiosk.Domain.Whatsapp;

namespace Kiosk.Api.Seed;

public sealed class SeedWorker : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SeedWorker> _logger;

    public SeedWorker(IServiceScopeFactory scopeFactory, ILogger<SeedWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var sp = scope.ServiceProvider;

            var comercios = sp.GetRequiredService<IComercioRepository>();
            if (await comercios.ExisteAlgunoAsync(cancellationToken))
            {
                return;
            }

            var configuration = sp.GetRequiredService<IConfiguration>();
            var seed = configuration.GetSection("Seed");

            var comercio = Comercio.Crear(seed["ComercioNombre"] ?? "Kiosco Demo");
            comercios.Add(comercio);

            var passwordHasher = sp.GetRequiredService<IPasswordHasher>();
            var usuarios = sp.GetRequiredService<IUsuarioRepository>();
            var usuario = Usuario.Crear(
                comercio.Id,
                seed["AdminNombre"] ?? "Administrador",
                seed["AdminUsername"] ?? "admin",
                passwordHasher.Hash(seed["AdminPassword"] ?? "admin123"),
                Rol.ADMIN);
            usuarios.Add(usuario);

            var whatsapp = seed["AdminWhatsapp"];
            if (!string.IsNullOrWhiteSpace(whatsapp))
            {
                var whitelist = sp.GetRequiredService<IWhatsAppWhitelistRepository>();
                whitelist.Add(WhatsappWhitelist.Autorizar(comercio.Id, whatsapp));
            }

            await sp.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seed completado: comercio '{Comercio}' y usuario '{Usuario}'.", comercio.Nombre, usuario.Username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo completar el seed. Verificá que las migraciones estén aplicadas.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
