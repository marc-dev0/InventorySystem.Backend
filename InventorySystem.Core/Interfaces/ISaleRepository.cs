using InventorySystem.Core.Entities;

namespace InventorySystem.Core.Interfaces;

public interface ISaleRepository : IRepository<Sale>
{
    Task<IEnumerable<Sale>> GetSalesByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<Sale>> GetSalesByCustomerAsync(int customerId);
    Task<Sale?> GetSaleDetailsAsync(int saleId);
    Task<string> GenerateSaleNumberAsync();
    Task<decimal> GetTotalSalesForDateAsync(DateTime date);
    Task<IEnumerable<object>> GetSalesReportAsync(DateTime startDate, DateTime endDate);
}
