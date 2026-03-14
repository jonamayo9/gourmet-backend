using GourmetApi.Entities;
using Microsoft.EntityFrameworkCore;

namespace GourmetApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Shift> Shifts => Set<Shift>();

    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    public DbSet<RestaurantTable> RestaurantTables { get; set; }
    public DbSet<TableSession> TableSessions { get; set; }
    public DbSet<TableSessionItem> TableSessionItems { get; set; }

    public DbSet<MercadoPagoCheckout> MercadoPagoCheckouts { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Company
        modelBuilder.Entity<Company>()
            .HasIndex(x => x.Slug)
            .IsUnique();

        // MenuItem
        modelBuilder.Entity<MenuItem>()
            .Property(x => x.Price)
            .HasColumnType("numeric(10,2)");

        // Shift
        modelBuilder.Entity<Shift>()
          .HasOne(s => s.Company)
          .WithMany(c => c.Shifts)
          .HasForeignKey(s => s.CompanyId);

        // AdminUser -> Company (CompanyId nullable; null = SuperAdmin)
        modelBuilder.Entity<AdminUser>()
            .HasOne(u => u.Company)
            .WithMany()
            .HasForeignKey(u => u.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // AdminUser
        modelBuilder.Entity<AdminUser>()
            .HasIndex(x => x.Email)
            .IsUnique();

        modelBuilder.Entity<AdminUser>()
            .HasOne(x => x.Company)
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // CompanyId + Email: útil para evitar duplicados por empresa si querés (opcional)
        // Si querés email único global, decime y lo hacemos único sin CompanyId.
        modelBuilder.Entity<AdminUser>()
            .HasIndex(x => x.Email);

        // Seed Turnos para como-en-casa (CompanyId = 1)
        modelBuilder.Entity<Shift>().HasData(
            new Shift
            {
                Id = 1,
                CompanyId = 1,
                DayOfWeek = null,
                OpenHour = 11,
                CloseHour = 15,
                Enabled = true,
                CreatedAt = DateTime.SpecifyKind(new DateTime(2026, 1, 1), DateTimeKind.Utc)
            },
            new Shift
            {
                Id = 2,
                CompanyId = 1,
                DayOfWeek = null,
                OpenHour = 19,
                CloseHour = 23,
                Enabled = true,
                CreatedAt = DateTime.SpecifyKind(new DateTime(2026, 1, 1), DateTimeKind.Utc)
            }
        );
    }
}