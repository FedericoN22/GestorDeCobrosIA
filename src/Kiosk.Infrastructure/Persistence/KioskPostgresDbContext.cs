using Microsoft.EntityFrameworkCore;

namespace Kiosk.Infrastructure.Persistence;

public sealed class KioskPostgresDbContext : KioskDbContext
{
    public KioskPostgresDbContext(DbContextOptions<KioskPostgresDbContext> options) : base(options)
    {
    }
}
