using Kiosk.Application.Puertos;
using Kiosk.Domain.Auditoria;
using Kiosk.Domain.Catalogos;
using Kiosk.Domain.Comercios;
using Kiosk.Domain.Configuracion;
using Kiosk.Domain.Stock;
using Kiosk.Domain.Sync;
using Kiosk.Domain.Usuarios;
using Kiosk.Domain.Ventas;
using Kiosk.Domain.Whatsapp;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Infrastructure.Persistence;

public class KioskDbContext : DbContext, IUnitOfWork
{
    public KioskDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Comercio> Comercios => Set<Comercio>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Presentacion> Presentaciones => Set<Presentacion>();
    public DbSet<MovimientoStock> MovimientosStock => Set<MovimientoStock>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Caja> Cajas => Set<Caja>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<Intencion> Intenciones => Set<Intencion>();
    public DbSet<WhatsappWhitelist> WhatsappWhitelist => Set<WhatsappWhitelist>();
    public DbSet<AuditoriaEvento> AuditoriaEventos => Set<AuditoriaEvento>();
    public DbSet<Configuracion> Configuraciones => Set<Configuracion>();
    public DbSet<OperacionSync> OperacionesSync => Set<OperacionSync>();

    Task<int> IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken)
        => base.SaveChangesAsync(cancellationToken);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Conventions.Add(_ => new SnakeCaseNamingConvention());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Comercio>(e =>
        {
            e.Property(c => c.Nombre).HasMaxLength(120).IsRequired();
        });

        modelBuilder.Entity<Categoria>(e =>
        {
            e.Property(c => c.Nombre).HasMaxLength(80).IsRequired();
            e.HasIndex(c => new { c.ComercioId, c.Nombre }).IsUnique();
            e.HasOne<Comercio>().WithMany().HasForeignKey(c => c.ComercioId);
        });

        modelBuilder.Entity<Producto>(e =>
        {
            e.Property(p => p.Nombre).HasMaxLength(120).IsRequired();
            e.Property(p => p.NombreNormalizado).HasMaxLength(120).IsRequired();
            e.HasIndex(p => new { p.ComercioId, p.NombreNormalizado });
            e.HasOne<Comercio>().WithMany().HasForeignKey(p => p.ComercioId);
            e.HasOne<Categoria>().WithMany().HasForeignKey(p => p.CategoriaId).IsRequired(false);
            e.HasMany(p => p.Presentaciones).WithOne().HasForeignKey(pr => pr.ProductoId);
        });

        modelBuilder.Entity<Presentacion>(e =>
        {
            e.ToTable("presentacion");
            e.Property(p => p.Nombre).HasMaxLength(80).IsRequired();
            e.Property(p => p.CodigoBarras).HasMaxLength(32);
            e.HasIndex(p => p.ProductoId);
            e.HasIndex(p => p.CodigoBarras);
        });

        modelBuilder.Entity<MovimientoStock>(e =>
        {
            e.Property(m => m.Motivo).HasMaxLength(200);
            e.HasIndex(m => new { m.PresentacionId, m.CreatedAt });
            e.HasIndex(m => m.VentaId);
            e.HasOne<Presentacion>().WithMany().HasForeignKey(m => m.PresentacionId);
            e.HasOne<Venta>().WithMany().HasForeignKey(m => m.VentaId).IsRequired(false);
        });

        modelBuilder.Entity<Usuario>(e =>
        {
            e.Property(u => u.Nombre).HasMaxLength(80).IsRequired();
            e.Property(u => u.Username).HasMaxLength(40).IsRequired();
            e.Property(u => u.PasswordHash).HasMaxLength(200).IsRequired();
            e.HasIndex(u => u.Username).IsUnique();
            e.HasOne<Comercio>().WithMany().HasForeignKey(u => u.ComercioId);
        });

        modelBuilder.Entity<Caja>(e =>
        {
            e.HasOne<Comercio>().WithMany().HasForeignKey(c => c.ComercioId);
            e.HasOne<Usuario>().WithMany().HasForeignKey(c => c.UsuarioId);
            e.HasIndex(c => c.ComercioId).IsUnique().HasFilter("\"estado\" = 1");
        });

        modelBuilder.Entity<Venta>(e =>
        {
            e.HasIndex(v => new { v.ComercioId, v.Fecha });
            e.HasIndex(v => v.CajaId);
            e.HasOne<Comercio>().WithMany().HasForeignKey(v => v.ComercioId);
            e.HasOne<Caja>().WithMany().HasForeignKey(v => v.CajaId);
            e.HasMany(v => v.Lineas).WithOne().HasForeignKey(l => l.VentaId);
            e.HasMany(v => v.Pagos).WithOne().HasForeignKey(p => p.VentaId);
        });

        modelBuilder.Entity<LineaVenta>(e =>
        {
            e.Property(l => l.ProductoNombre).HasMaxLength(120).IsRequired();
            e.Property(l => l.PresentacionNombre).HasMaxLength(80).IsRequired();
            e.HasOne<Presentacion>().WithMany().HasForeignKey(l => l.PresentacionId);
        });

        modelBuilder.Entity<Pago>(e =>
        {
        });

        modelBuilder.Entity<Intencion>(e =>
        {
            e.Property(i => i.WhatsappNumero).HasMaxLength(20).IsRequired();
            e.Property(i => i.TextoOriginal).IsRequired();
            e.Property(i => i.Decision).HasMaxLength(100);
            e.HasIndex(i => new { i.WhatsappNumero, i.Estado });
            e.HasOne<Comercio>().WithMany().HasForeignKey(i => i.ComercioId);
        });

        modelBuilder.Entity<WhatsappWhitelist>(e =>
        {
            e.Property(w => w.WhatsappNumero).HasMaxLength(20).IsRequired();
            e.HasIndex(w => new { w.ComercioId, w.WhatsappNumero }).IsUnique();
            e.HasOne<Comercio>().WithMany().HasForeignKey(w => w.ComercioId);
        });

        modelBuilder.Entity<AuditoriaEvento>(e =>
        {
            e.Property(a => a.Actor).HasMaxLength(80).IsRequired();
            e.Property(a => a.Tipo).HasMaxLength(50).IsRequired();
            e.HasOne<Comercio>().WithMany().HasForeignKey(a => a.ComercioId);
        });

        modelBuilder.Entity<Configuracion>(e =>
        {
            e.HasKey(c => new { c.ComercioId, c.Clave });
            e.Property(c => c.Clave).HasMaxLength(80).IsRequired();
            e.HasOne<Comercio>().WithMany().HasForeignKey(c => c.ComercioId);
        });

        modelBuilder.Entity<OperacionSync>(e =>
        {
            e.Property(o => o.Tipo).HasMaxLength(50).IsRequired();
            e.HasIndex(o => new { o.ComercioId, o.OperationId }).IsUnique();
            e.HasIndex(o => new { o.ComercioId, o.AplicadaEn });
            e.HasOne<Comercio>().WithMany().HasForeignKey(o => o.ComercioId);
        });
    }
}
