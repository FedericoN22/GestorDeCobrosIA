using Microsoft.EntityFrameworkCore;

namespace Kiosk.Infrastructure.Persistence;

public sealed class KioskSqliteDbContext : KioskDbContext
{
    public KioskSqliteDbContext(DbContextOptions<KioskSqliteDbContext> options) : base(options)
    {
    }
}
