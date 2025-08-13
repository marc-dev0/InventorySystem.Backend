using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using InventorySystem.Core.Entities;
using InventorySystem.Core.Interfaces;
using InventorySystem.Infrastructure.Data;

namespace InventorySystem.Infrastructure.Repositories;

public class SaleRepository : Repository<Sale>, ISaleRepository
{
    public SaleRepository(InventoryDbContext context, ILogger<Repository<Sale>> logger) : base(context, logger)
    {
    }

    public async Task<IEnumerable<Sale>> GetSalesByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Include(s => s.Customer)
            .Include(s => s.Details)
                .ThenInclude(d => d.Product)
            .Where(s => s.SaleDate.Date >= startDate.Date && 
                       s.SaleDate.Date <= endDate.Date)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Sale>> GetSalesByCustomerAsync(int customerId)
    {
        return await _dbSet
            .Include(s => s.Customer)
            .Include(s => s.Details)
                .ThenInclude(d => d.Product)
            .Where(s => s.CustomerId == customerId)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync();
    }

    public async Task<Sale?> GetSaleDetailsAsync(int saleId)
    {
        return await _dbSet
            .Include(s => s.Customer)
            .Include(s => s.Details)
                .ThenInclude(d => d.Product)
                    .ThenInclude(p => p.Category)
            .FirstOrDefaultAsync(s => s.Id == saleId);
    }

    public async Task<string> GenerateSaleNumberAsync()
    {
        var today = DateTime.Now;
        var lastSale = await _dbSet
            .Where(s => s.SaleDate.Date == today.Date)
            .OrderByDescending(s => s.Id)
            .FirstOrDefaultAsync();

        var number = 1;
        if (lastSale != null)
        {
            var lastNumber = lastSale.SaleNumber.Split('-').LastOrDefault();
            if (int.TryParse(lastNumber, out var num))
                number = num + 1;
        }

        return $"S-{today:yyyyMMdd}-{number:D4}";
    }

    public async Task<decimal> GetTotalSalesForDateAsync(DateTime date)
    {
        return await _dbSet
            .Where(s => s.SaleDate.Date == date.Date)
            .SumAsync(s => s.Total);
    }

    public async Task<IEnumerable<object>> GetSalesReportAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Where(s => s.SaleDate.Date >= startDate.Date && 
                       s.SaleDate.Date <= endDate.Date)
            .GroupBy(s => s.SaleDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                TotalSales = g.Count(),
                TotalAmount = g.Sum(s => s.Total)
            })
            .OrderBy(x => x.Date)
            .ToListAsync();
    }
}
