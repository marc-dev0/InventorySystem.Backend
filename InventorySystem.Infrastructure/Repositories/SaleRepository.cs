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

    public override async Task<IEnumerable<Sale>> GetAllAsync()
    {
        return await _dbSet
            .Include(s => s.Customer)
            .Include(s => s.Store)
            .Include(s => s.Details)
                .ThenInclude(d => d.Product)
                    .ThenInclude(p => p.Category)
            .Include(s => s.Details)
                .ThenInclude(d => d.Product)
                    .ThenInclude(p => p.Brand)
            .ToListAsync();
    }

    public async Task<(IEnumerable<Sale> Sales, int TotalCount)> GetPaginatedAsync(int page, int pageSize, string search = "", string storeCode = "")
    {
        var query = _dbSet
            .Include(s => s.Customer)
            .Include(s => s.Store)
            .Include(s => s.Details)
                .ThenInclude(d => d.Product)
            .AsQueryable();

        // Apply search filter
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(s => 
                s.SaleNumber.Contains(search) ||
                (s.Customer != null && s.Customer.Name.Contains(search)) ||
                (s.Store != null && s.Store.Name.Contains(search)));
        }

        // Apply store filter
        if (!string.IsNullOrEmpty(storeCode))
        {
            query = query.Where(s => s.Store != null && s.Store.Code == storeCode);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply pagination
        var sales = await query
            .OrderByDescending(s => s.SaleDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (sales, totalCount);
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
