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
            
            // Optional Employee relationship
            entity.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);
            
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


        // Seed critical system configurations
        modelBuilder.Entity<SystemConfiguration>().HasData(
            new SystemConfiguration
            {
                Id = 1,
                ConfigKey = "GLOBAL_MINIMUM_STOCK",
                ConfigValue = "5",
                ConfigType = "Number",
                Category = "INVENTORY",
                Description = "Stock mínimo global para considerar productos con stock bajo",
                Active = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new SystemConfiguration
            {
                Id = 2,
                ConfigKey = "STOCK_INITIAL_VALIDATION",
                ConfigValue = "true",
                ConfigType = "Boolean",
                Category = "IMPORT",
                Description = "Validar que solo se permita una carga de stock inicial por tienda",
                Active = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new SystemConfiguration
            {
                Id = 3,
                ConfigKey = "SALES_GROUPING_COLUMN",
                ConfigValue = "SaleNumber",
                ConfigType = "String",
                Category = "IMPORT",
                Description = "Columna por la cual agrupar las ventas (SaleNumber = columna F)",
                Active = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new SystemConfiguration
            {
                Id = 4,
                ConfigKey = "IMPORT_COLUMNS_SALES",
                ConfigValue = "[\"Razón Social\",\"Empleado Venta\",\"Almacén\",\"Cliente Nombre\",\"Cliente Doc.\",\"#-DOC\",\"# Doc. Relacionado\",\"Fecha\",\"Hora\",\"Tip. Doc.\",\"Unidad\",\"Cantidad\",\"Precio de Venta\",\"IGV\",\"Total\",\"Descuento aplicado (%)\",\"Conversión\",\"Moneda\",\"Codigo SKU\",\"Cod. alternativo\",\"Marca\",\"Categoría\",\"Características\",\"Nombre\",\"Descripción\",\"Proveedor\",\"Precio de costo\",\"Empleado registro\"]",
                ConfigType = "JSON",
                Category = "IMPORT",
                Description = "Columnas esperadas para importación de ventas en Excel (agrupadas por Número de Venta columna F)",
                Active = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new SystemConfiguration
            {
                Id = 5,
                ConfigKey = "IMPORT_MAPPING_SALES",
                ConfigValue = "{\"RazonSocialColumn\":1,\"EmpleadoVentaColumn\":2,\"AlmacenColumn\":3,\"ClienteNombreColumn\":4,\"ClienteDocColumn\":5,\"NumDocColumn\":6,\"DocRelacionadoColumn\":7,\"FechaColumn\":8,\"HoraColumn\":9,\"TipDocColumn\":10,\"UnidadColumn\":11,\"CantidadColumn\":12,\"PrecioVentaColumn\":13,\"IgvColumn\":14,\"TotalColumn\":15,\"DescuentoColumn\":16,\"ConversionColumn\":17,\"MonedaColumn\":18,\"CodigoSkuColumn\":19,\"CodAlternativoColumn\":20,\"MarcaColumn\":21,\"CategoriaColumn\":22,\"CaracteristicasColumn\":23,\"NombreColumn\":24,\"DescripcionColumn\":25,\"ProveedorColumn\":26,\"PrecioCostoColumn\":27,\"EmpleadoRegistroColumn\":28,\"StartRow\":2}",
                ConfigType = "Json",
                Category = "Import",
                Description = "Mapeo de posiciones de columnas para importación de ventas",
                Active = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new SystemConfiguration
            {
                Id = 6,
                ConfigKey = "IMPORT_COLUMNS_PRODUCT",
                ConfigValue = "[\"Tienda\",\"Código\",\"Cod. barras\",\"Nombre\",\"Descripción\",\"Categorias\",\"Marca\",\"Características\",\"Impuestos\",\"P. costo\",\"Estado\",\"Stock\",\"Stock min\",\"Ubicación\",\"P. venta\",\"Unidad\",\"Nombre de lista de precio\",\"Factor de conversión\",\"Precio al por mayor\",\"Cantidad mínima\",\"Cantidad máxima\"]",
                ConfigType = "JSON",
                Category = "IMPORT",
                Description = "Columnas esperadas para importación de productos en Excel",
                Active = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new SystemConfiguration
            {
                Id = 7,
                ConfigKey = "IMPORT_COLUMNS_STOCK",
                ConfigValue = "[\"StoreCode\",\"ProductCode\",\"Stock\"]",
                ConfigType = "JSON",
                Category = "IMPORT",
                Description = "Columnas esperadas para importación de stock inicial en Excel",
                Active = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new SystemConfiguration
            {
                Id = 8,
                ConfigKey = "IMPORT_MAPPING_STOCK",
                ConfigValue = "{\"StoreCode\":0,\"ProductCode\":1,\"Stock\":2}",
                ConfigType = "JSON",
                Category = "IMPORT",
                Description = "Mapeo de columnas para importación de stock (números de columna)",
                Active = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new SystemConfiguration
            {
                Id = 9,
                ConfigKey = "DEFAULT_STORE_CONFIG",
                ConfigValue = "{\"Code\": \"TIETAN\", \"Name\": \"Tienda Tantamayo\", \"Address\": \"Dirección Principal\", \"Phone\": \"123456789\", \"Active\": true}",
                ConfigType = "JSON",
                Category = "STORE",
                Description = "Configuración de tienda por defecto para el sistema",
                Active = true,
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