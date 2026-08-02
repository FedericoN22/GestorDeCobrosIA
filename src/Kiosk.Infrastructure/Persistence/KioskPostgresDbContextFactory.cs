using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kiosk.Infrastructure.Persistence;

public sealed class KioskPostgresDbContextFactory : IDesignTimeDbContextFactory<KioskPostgresDbContext>
{
    public KioskPostgresDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<KioskPostgresDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=kiosk;Username=kiosk;Password=kiosk");
        return new KioskPostgresDbContext(optionsBuilder.Options);
    }
}
