using InventorySystem.Core.Entities;

namespace InventorySystem.Core.Interfaces;

public interface IProductStockRepository : IRepository<ProductStock>
{
    Task<ProductStock?> GetByProductAndStoreAsync(int productId, int storeId);
    Task<IEnumerable<ProductStock>> GetByProductIdAsync(int productId);
    Task<IEnumerable<ProductStock>> GetByStoreIdAsync(int storeId);
    Task<IEnumerable<ProductStock>> GetLowStockAsync(int storeId);
    Task<decimal> GetTotalStockForProductAsync(int productId);
    Task<bool> HasStockForStoreAsync(int storeId);
}