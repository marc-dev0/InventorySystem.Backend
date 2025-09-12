using Microsoft.EntityFrameworkCore;
using InventorySystem.Core.Entities;

namespace InventorySystem.Infrastructure.Data;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Disable pending model changes warning for development
        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    // DbSets
    public DbSet<Category> Categories { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<Brand> Brands { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Sale> Sales { get; set; }
    public DbSet<SaleDetail> SaleDetails { get; set; }
    public DbSet<Purchase> Purchases { get; set; }
    public DbSet<PurchaseDetail> PurchaseDetails { get; set; }
    public DbSet<InventoryMovement> InventoryMovements { get; set; }
    public DbSet<Store> Stores { get; set; }
    public DbSet<ProductStock> ProductStocks { get; set; }
    public DbSet<ImportBatch> ImportBatches { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<BackgroundJob> BackgroundJobs { get; set; }
    public DbSet<SystemConfiguration> SystemConfigurations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Basic configurations only
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<Brand>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        });


        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PurchasePrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.SalePrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Stock).HasColumnType("decimal(18,3)");
            entity.Property(e => e.MinimumStock).HasColumnType("decimal(18,3)");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Document).HasMaxLength(50);
            entity.Property(e => e.Position).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.HasIndex(e => e.Code).IsUnique();
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SaleNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.SubTotal).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Taxes).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Total).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Purchase>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PurchaseNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.SubTotal).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Taxes).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Total).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Store>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<ProductStock>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CurrentStock).HasColumnType("decimal(18,3)");
            entity.Property(e => e.MinimumStock).HasColumnType("decimal(18,3)");
            entity.Property(e => e.MaximumStock).HasColumnType("decimal(18,3)");
            entity.Property(e => e.AverageCost).HasColumnType("decimal(18,4)");
            entity.HasIndex(e => new { e.ProductId, e.StoreId }).IsUnique();
            
            entity.HasOne(e => e.Product)
                .WithMany(p => p.ProductStocks)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Store)
                .WithMany(s => s.ProductStocks)
                .HasForeignKey(e => e.StoreId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SaleDetail>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasColumnType("decimal(18,3)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Subtotal).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<PurchaseDetail>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasColumnType("decimal(18,3)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Subtotal).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<InventoryMovement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasColumnType("decimal(18,3)");
            entity.Property(e => e.PreviousStock).HasColumnType("decimal(18,3)");
            entity.Property(e => e.NewStock).HasColumnType("decimal(18,3)");
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18,4)");
            entity.Property(e => e.TotalCost).HasColumnType("decimal(18,2)");
            
            entity.HasOne(e => e.Store)
                .WithMany(s => s.InventoryMovements)
                .HasForeignKey(e => e.StoreId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.ProductStock)
                .WithMany(ps => ps.InventoryMovements)
                .HasForeignKey(e => e.ProductStockId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ImportBatch>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BatchType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.StoreCode).HasMaxLength(10);
            entity.Property(e => e.ImportedBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DeletedBy).HasMaxLength(100);
            entity.Property(e => e.DeleteReason).HasMaxLength(500);
            
            entity.HasMany(e => e.Sales)
                .WithOne(s => s.ImportBatch)
                .HasForeignKey(s => s.ImportBatchId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasMany(e => e.Products)
                .WithOne(p => p.ImportBatch)
                .HasForeignKey(p => p.ImportBatchId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasMany(e => e.ProductStocks)
                .WithOne(ps => ps.ImportBatch)
                .HasForeignKey(ps => ps.ImportBatchId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Seed data
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Electronics", Description = "Electronic devices" },
            new Category { Id = 2, Name = "Clothing", Description = "Apparel items" }
        );

        modelBuilder.Entity<Supplier>().HasData(
            new Supplier { Id = 1, Name = "TechSupply Inc.", Phone = "555-0001" },
            new Supplier { Id = 2, Name = "Fashion World", Phone = "555-0002" }
        );

        modelBuilder.Entity<Store>().HasData(
            new Store { Id = 1, Code = "TANT", Name = "Tienda Tantamayo", Description = "Main Tantamayo Store", Active = true },
            new Store { Id = 2, Code = "MAIN", Name = "Main Warehouse", Description = "Central warehouse", Active = true }
        );


        // Seed default admin user
        modelBuilder.Entity<User>().HasData(
            new User 
            { 
                Id = 1, 
                Username = "admin",
                Email = "admin@inventorysystem.com",
                FirstName = "Administrator",
                LastName = "System",
                Role = "Admin",
                IsActive = true,
                PasswordHash = "zbFyeIKv6pKbhTL3XWaVhp5xzKF6oF8Kt7lEI8MEKy0=", // Password: admin123
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity && 
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (BaseEntity)entry.Entity;
            var utcNow = DateTime.UtcNow;

            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = utcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entity.UpdatedAt = utcNow;
            }
        }
    }
}