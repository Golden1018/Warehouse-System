// ============================================================
// Data/WarehouseDbContext.cs
// ------------------------------------------------------------
// DbContext is the main class that talks to the database.
// Think of it as a "smart connection" that:
//   • Knows which tables exist (via DbSet<T> properties).
//   • Translates LINQ queries into SQL for us.
//   • Tracks changes and saves them with SaveChangesAsync().
//
// We inherit from DbContext (provided by EF Core) and add
// our own tables as DbSet properties.
// ============================================================

using Microsoft.EntityFrameworkCore;
using SimpleWMS.Models;

namespace SimpleWMS.Data;

public class WarehouseDbContext : DbContext
{
    // The constructor receives options (e.g. which database
    // file to use) and passes them up to the base class.
    // ASP.NET Core's Dependency Injection will call this
    // constructor and supply the options automatically.
    public WarehouseDbContext(DbContextOptions<WarehouseDbContext> options)
        : base(options) { }

    // DbSet<T> = a table in the database.
    // EF Core uses these to generate the tables.
    public DbSet<Product>   Products  => Set<Product>();
    public DbSet<StockLog>  StockLogs => Set<StockLog>();
    public DbSet<Location>  Locations => Set<Location>();

    // OnModelCreating lets us fine-tune how EF maps our
    // classes to the database — this is called Fluent API.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── Product table configuration ───────────────────
        modelBuilder.Entity<Product>(entity =>
        {
            // SKU must be unique — no two products can share
            // the same code.
            entity.HasIndex(p => p.SKU)
                  .IsUnique();

            // SKU and Name are required fields (NOT NULL).
            entity.Property(p => p.SKU)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.Property(p => p.Name)
                  .IsRequired()
                  .HasMaxLength(200);
        });

        // ── Location table configuration ──────────────────
        modelBuilder.Entity<Location>(entity =>
        {
            // ShelfCode must be unique — no two shelves share a code.
            entity.HasIndex(l => l.ShelfCode)
                  .IsUnique();

            entity.Property(l => l.ShelfCode)
                  .IsRequired()
                  .HasMaxLength(20);

            entity.Property(l => l.Description)
                  .HasMaxLength(200);

            // One Location → many Products
            // If a location is deleted, set product's LocationId to null
            // (don't delete the products themselves).
            entity.HasMany(l => l.Products)
                  .WithOne(p => p.Location)
                  .HasForeignKey(p => p.LocationId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ── StockLog table configuration ──────────────────
        modelBuilder.Entity<StockLog>(entity =>
        {
            // Type column stores "In" or "Out" — short string.
            entity.Property(s => s.Type)
                  .IsRequired()
                  .HasMaxLength(3);

            // Define the one-to-many relationship:
            //   One Product → many StockLogs
            // If a Product is deleted, restrict (don't delete logs).
            entity.HasOne(s => s.Product)
                  .WithMany(p => p.StockLogs)
                  .HasForeignKey(s => s.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
