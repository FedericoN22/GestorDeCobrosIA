using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kiosk.Infrastructure.Persistence;

public sealed class KioskSqliteDbContextFactory : IDesignTimeDbContextFactory<KioskSqliteDbContext>
{
    public KioskSqliteDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<KioskSqliteDbContext>();
        optionsBuilder.UseSqlite("Data Source=kiosk.db");
        return new KioskSqliteDbContext(optionsBuilder.Options);
    }
}
