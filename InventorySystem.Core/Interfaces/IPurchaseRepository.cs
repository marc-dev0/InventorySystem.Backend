using InventorySystem.Core.Entities;

namespace InventorySystem.Core.Interfaces;

public interface IPurchaseRepository : IRepository<Purchase>
{
    Task<IEnumerable<Purchase>> GetPurchasesByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<Purchase?> GetPurchaseDetailsAsync(int purchaseId);
    Task<string> GeneratePurchaseNumberAsync();
    Task<decimal> GetTotalPurchasesForDateAsync(DateTime date);
}
