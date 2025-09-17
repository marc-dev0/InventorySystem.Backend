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

    public override async Task<IEnumerable<ProductStock>> GetAllAsync()
    {
        return await _context.Set<ProductStock>()
            .Include(ps => ps.Product)
                .ThenInclude(p => p.Category)
            .Include(ps => ps.Product)
                .ThenInclude(p => p.Brand)
            .Include(ps => ps.Store)
            .Where(ps => ps.Product.Active && !ps.IsDeleted) // Only active products and non-deleted ProductStocks
            .ToListAsync();
    }

    public async Task<(IEnumerable<ProductStock> Items, int TotalCount)> GetPaginatedAsync(int page, int pageSize, string search = "", string storeCode = "")
    {
        var query = _context.Set<ProductStock>()
            .Include(ps => ps.Product)
                .ThenInclude(p => p.Category)
            .Include(ps => ps.Product)
                .ThenInclude(p => p.Brand)
            .Include(ps => ps.Store)
            .Where(ps => ps.Product.Active && !ps.IsDeleted) // Only active products and non-deleted ProductStocks
            .AsQueryable();

        // Apply search filter
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(ps =>
                ps.Product.Code.Contains(search) ||
                ps.Product.Name.Contains(search) ||
                ps.Store.Name.Contains(search) ||
                ps.Store.Code.Contains(search) ||
                (ps.Product.Category != null && ps.Product.Category.Name.Contains(search)) ||
                (ps.Product.Brand != null && ps.Product.Brand.Name.Contains(search)));
        }

        // Apply store filter
        if (!string.IsNullOrEmpty(storeCode))
        {
            query = query.Where(ps => ps.Store.Code == storeCode);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply pagination
        var items = await query
            .OrderBy(ps => ps.Store.Name)
            .ThenBy(ps => ps.Product.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
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
            .Where(ps => ps.StoreId == storeId && ps.Product.Active && !ps.IsDeleted)
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
            .Where(ps => ps.ProductId == productId && !ps.IsDeleted)
            .SumAsync(ps => ps.CurrentStock);
    }

    public async Task<bool> HasStockForStoreAsync(int storeId)
    {
        return await _context.Set<ProductStock>()
            .AnyAsync(ps => ps.StoreId == storeId && !ps.IsDeleted);
    }
}