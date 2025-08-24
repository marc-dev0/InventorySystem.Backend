using Microsoft.EntityFrameworkCore;
using InventorySystem.Infrastructure.Data;

var connectionString = "Host=localhost;Port=5432;Database=inventory_db;Username=marc;Password=11223344";

var options = new DbContextOptionsBuilder<InventoryDbContext>()
    .UseNpgsql(connectionString)
    .Options;

using var context = new InventoryDbContext(options);

try
{
    Console.WriteLine("Creating database...");
    bool created = context.Database.EnsureCreated();
    
    if (created)
    {
        Console.WriteLine("✅ Database created successfully!");
    }
    else
    {
        Console.WriteLine("✅ Database already exists!");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error creating database: {ex.Message}");
}