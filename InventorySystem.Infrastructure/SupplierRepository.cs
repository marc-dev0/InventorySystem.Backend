using Microsoft.EntityFrameworkCore;
using InventorySystem.Core.Entities;
using InventorySystem.Core.Interfaces;
using InventorySystem.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace InventorySystem.Infrastructure.Repositories;

public class SupplierRepository : Repository<Supplier>, ISupplierRepository
{
    public SupplierRepository(InventoryDbContext context, ILogger<SupplierRepository> logger) : base(context, logger)
    {
    }

    public async Task<IEnumerable<Supplier>> GetActiveSuppliersAsync()
    {
        try
        {
            _logger.LogDebug("Getting active suppliers");
            var suppliers = await _dbSet
                .Where(s => s.Active)
                .OrderBy(s => s.Name)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} active suppliers", suppliers.Count);
            return suppliers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active suppliers");
            throw;
        }
    }

    public async Task<bool> HasProductsAsync(int supplierId)
    {
        try
        {
            _logger.LogDebug("Checking if supplier {SupplierId} has products", supplierId);
            var hasProducts = await _context.Products
                .AnyAsync(p => p.SupplierId == supplierId && !p.IsDeleted);

            _logger.LogDebug("Supplier {SupplierId} has products: {HasProducts}", supplierId, hasProducts);
            return hasProducts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if supplier {SupplierId} has products", supplierId);
            throw;
        }
    }

    public async Task<Supplier?> GetByNameAsync(string name)
    {
        try
        {
            _logger.LogDebug("Getting supplier by name: {Name}", name);
            var supplier = await _dbSet
                .FirstOrDefaultAsync(s => s.Name.ToLower() == name.ToLower());

            if (supplier == null)
            {
                _logger.LogDebug("Supplier with name {Name} not found", name);
            }

            return supplier;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supplier by name: {Name}", name);
            throw;
        }
    }
}
