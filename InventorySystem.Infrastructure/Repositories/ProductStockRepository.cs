using InventorySystem.Core.Entities;
using InventorySystem.Core.Interfaces;
using InventorySystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InventorySystem.Infrastructure.Repositories;

public class ProductStockRepository : Repository<ProductStock>, IProductStockRepository
{
    public ProductStockRepository(InventoryDbContext context, ILogger<Repository<ProductStock>> logger) 
        : base(context, logger)
    {
    }

    public async Task<ProductStock?> GetByProductAndStoreAsync(int productId, int storeId)
    {
        return await _context.Set<ProductStock>()
            .Include(ps => ps.Product)
            .Include(ps => ps.Store)
            .FirstOrDefaultAsync(ps => ps.ProductId == productId && ps.StoreId == storeId);
    }

    public async Task<IEnumerable<ProductStock>> GetByProductIdAsync(int productId)
    {
        return await _context.Set<ProductStock>()
            .Include(ps => ps.Store)
            .Where(ps => ps.ProductId == productId)
            .ToListAsync();
    }

    public async Task<IEnumerable<ProductStock>> GetByStoreIdAsync(int storeId)
    {
        return await _context.Set<ProductStock>()
            .Include(ps => ps.Product)
            .Where(ps => ps.StoreId == storeId)
            .ToListAsync();
    }

    public async Task<IEnumerable<ProductStock>> GetLowStockAsync(int storeId)
    {
        return await _context.Set<ProductStock>()
            .Include(ps => ps.Product)
            .Where(ps => ps.StoreId == storeId && ps.CurrentStock <= ps.MinimumStock)
            .ToListAsync();
    }

    public async Task<decimal> GetTotalStockForProductAsync(int productId)
    {
        return await _context.Set<ProductStock>()
            .Where(ps => ps.ProductId == productId)
            .SumAsync(ps => ps.CurrentStock);
    }
}