using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using InventorySystem.Core.Entities;
using InventorySystem.Core.Interfaces;
using InventorySystem.Infrastructure.Data;

namespace InventorySystem.Infrastructure.Repositories;

public class InventoryMovementRepository : Repository<InventoryMovement>, IInventoryMovementRepository
{
    public InventoryMovementRepository(InventoryDbContext context, ILogger<Repository<InventoryMovement>> logger) 
        : base(context, logger)
    {
    }

    public async Task<IEnumerable<InventoryMovement>> GetMovementsByProductAsync(int productId)
    {
        return await _dbSet
            .Include(m => m.Product)
            .Include(m => m.Sale)
            .Include(m => m.Purchase)
            .Where(m => m.ProductId == productId)
            .OrderByDescending(m => m.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<InventoryMovement>> GetMovementsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Include(m => m.Product)
            .Include(m => m.Sale)
            .Include(m => m.Purchase)
            .Where(m => m.Date.Date >= startDate.Date && m.Date.Date <= endDate.Date)
            .OrderByDescending(m => m.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<InventoryMovement>> GetMovementsByTypeAsync(MovementType type)
    {
        return await _dbSet
            .Include(m => m.Product)
            .Include(m => m.Sale)
            .Include(m => m.Purchase)
            .Where(m => m.Type == type)
            .OrderByDescending(m => m.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<InventoryMovement>> GetMovementsBySourceAsync(string source)
    {
        return await _dbSet
            .Include(m => m.Product)
            .Include(m => m.Sale)
            .Include(m => m.Purchase)
            .Where(m => m.Source == source)
            .OrderByDescending(m => m.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<InventoryMovement>> GetRecentMovementsAsync(int count = 50)
    {
        return await _dbSet
            .Include(m => m.Product)
            .Include(m => m.Sale)
            .Include(m => m.Purchase)
            .OrderByDescending(m => m.Date)
            .Take(count)
            .ToListAsync();
    }
}