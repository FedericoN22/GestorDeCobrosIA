using Kiosk.Application.Puertos;
using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Infrastructure.Persistence;
using Kiosk.Infrastructure.Persistence.Repositorios;
using Kiosk.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kiosk.Infrastructure;

public sealed record DbOptions(bool UseSqlite, string ConnectionString);

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, DbOptions options)
    {
        if (options.UseSqlite)
        {
            services.AddDbContext<KioskSqliteDbContext>(builder => builder.UseSqlite(options.ConnectionString));
            services.AddScoped<KioskDbContext>(sp => sp.GetRequiredService<KioskSqliteDbContext>());
        }
        else
        {
            services.AddDbContext<KioskPostgresDbContext>(builder => builder.UseNpgsql(options.ConnectionString));
            services.AddScoped<KioskDbContext>(sp => sp.GetRequiredService<KioskPostgresDbContext>());
        }

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<KioskDbContext>());
        services.AddScoped<IComercioRepository, ComercioRepository>();
        services.AddScoped<ICategoriaRepository, CategoriaRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IStockLedger, StockLedger>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<ICajaRepository, CajaRepository>();
        services.AddScoped<IVentaRepository, VentaRepository>();
        services.AddScoped<IIntencionRepository, IntencionRepository>();
        services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();
        services.AddScoped<IConfiguracionRepository, ConfiguracionRepository>();
        services.AddScoped<IWhatsAppWhitelistRepository, WhatsAppWhitelistRepository>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();

        return services;
    }
}
