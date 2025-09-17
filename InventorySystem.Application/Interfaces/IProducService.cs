using InventorySystem.Application.DTOs;

namespace InventorySystem.Application.Interfaces;

public interface IProductService
{
    Task<IEnumerable<ProductDto>> GetAllAsync();
    Task<PaginatedResponseDto<ProductDto>> GetPaginatedAsync(int page, int pageSize, string search = "", string categoryId = "", bool? lowStock = null, string status = "");
    Task<ProductDto?> GetByIdAsync(int id);
    Task<ProductDto?> GetByCodeAsync(string code);
    Task<IEnumerable<ProductDto>> GetByCategoryAsync(int categoryId);
    Task<IEnumerable<ProductDto>> GetLowStockProductsAsync();
    Task<IEnumerable<ProductDto>> SearchProductsAsync(string searchTerm);
    Task<ProductDto> CreateAsync(CreateProductDto dto);
    Task UpdateAsync(int id, UpdateProductDto dto);
    Task DeleteAsync(int id);
    Task UpdateStockAsync(int id, decimal newStock, string? reason);
    Task<object> GetProductStatsAsync(string search = "", string categoryId = "", bool? lowStock = null, string status = "");
}
