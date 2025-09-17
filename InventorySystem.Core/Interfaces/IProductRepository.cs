using InventorySystem.Core.Entities;

namespace InventorySystem.Core.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<(IEnumerable<Product> Products, int TotalCount)> GetPaginatedAsync(int page, int pageSize, string search = "", string categoryId = "");
    Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId);
    Task<IEnumerable<Product>> GetBySupplierAsync(int supplierId);
    Task<IEnumerable<Product>> GetLowStockProductsAsync();
    Task<Product?> GetByCodeAsync(string code);
    Task<bool> CodeExistsAsync(string code, int? excludeId = null);
    Task UpdateStockAsync(int productId, decimal newStock);
    Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm);
}
