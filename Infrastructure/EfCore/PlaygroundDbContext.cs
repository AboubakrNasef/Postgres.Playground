using Microsoft.EntityFrameworkCore;
using Scenarios.Domain.Entities;

namespace Scenarios.Infrastructure.EfCore;

public sealed class PlaygroundDbContext : DbContext
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Inventory> Inventories => Set<Inventory>();

    public PlaygroundDbContext(DbContextOptions<PlaygroundDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Account>(e =>
        {
            e.ToTable("accounts");
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).HasColumnName("id").ValueGeneratedOnAdd();
            e.Property(a => a.Name).HasColumnName("name").HasMaxLength(100);
            e.Property(a => a.Balance).HasColumnName("balance").HasColumnType("decimal(10,2)");
            e.Property(a => a.Version).HasColumnName("version");
        });

        mb.Entity<Order>(e =>
        {
            e.ToTable("orders");
            e.HasKey(o => o.Id);
            e.Property(o => o.Id).HasColumnName("id").ValueGeneratedOnAdd();
            e.Property(o => o.UserId).HasColumnName("user_id");
            e.Property(o => o.Amount).HasColumnName("amount").HasColumnType("decimal(10,2)");
            e.Property(o => o.Status).HasColumnName("status").HasMaxLength(20);
            e.Property(o => o.CreatedAt).HasColumnName("created_at");
        });

        mb.Entity<Inventory>(e =>
        {
            e.ToTable("inventory");
            e.HasKey(i => i.Id);
            e.Property(i => i.Id).HasColumnName("id").ValueGeneratedOnAdd();
            e.Property(i => i.ProductName).HasColumnName("product_name").HasMaxLength(100);
            e.Property(i => i.Quantity).HasColumnName("quantity");
        });
    }
}
