using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using InventorySystem.Core.Entities;
using InventorySystem.Core.Interfaces;
using InventorySystem.Infrastructure.Data;

namespace InventorySystem.Infrastructure.Repositories;

public class PurchaseRepository : Repository<Purchase>, IPurchaseRepository
{
    public PurchaseRepository(InventoryDbContext context, ILogger<Repository<Purchase>> logger) : base(context, logger)
    {
    }

    public async Task<IEnumerable<Purchase>> GetPurchasesByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Include(p => p.Details)
                .ThenInclude(d => d.Product)
            .Include(p => p.Details)
                .ThenInclude(d => d.Supplier)
            .Where(p => p.PurchaseDate.Date >= startDate.Date && 
                       p.PurchaseDate.Date <= endDate.Date)
            .OrderByDescending(p => p.PurchaseDate)
            .ToListAsync();
    }

    public async Task<Purchase?> GetPurchaseDetailsAsync(int purchaseId)
    {
        return await _dbSet
            .Include(p => p.Details)
                .ThenInclude(d => d.Product)
                    .ThenInclude(p => p.Category)
            .Include(p => p.Details)
                .ThenInclude(d => d.Supplier)
            .FirstOrDefaultAsync(p => p.Id == purchaseId);
    }

    public async Task<string> GeneratePurchaseNumberAsync()
    {
        var today = DateTime.Now;
        var lastPurchase = await _dbSet
            .Where(p => p.PurchaseDate.Date == today.Date)
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync();

        var number = 1;
        if (lastPurchase != null)
        {
            var lastNumber = lastPurchase.PurchaseNumber.Split('-').LastOrDefault();
            if (int.TryParse(lastNumber, out var num))
                number = num + 1;
        }

        return $"P-{today:yyyyMMdd}-{number:D4}";
    }

    public async Task<decimal> GetTotalPurchasesForDateAsync(DateTime date)
    {
        return await _dbSet
            .Where(p => p.PurchaseDate.Date == date.Date)
            .SumAsync(p => p.Total);
    }
}
