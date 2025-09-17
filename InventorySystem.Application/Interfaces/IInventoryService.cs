using InventorySystem.Application.DTOs;

namespace InventorySystem.Application.Interfaces;

public interface IInventoryService
{
    Task<PaginatedResponseDto<InventoryItemDto>> GetPaginatedAsync(int page, int pageSize, string search = "", string storeCode = "", bool? lowStock = null);
    Task<IEnumerable<InventoryItemDto>> GetAllAsync();
    Task<IEnumerable<InventoryItemDto>> GetByStoreAsync(int storeId);
    Task<IEnumerable<InventoryItemDto>> GetLowStockItemsAsync();
    Task<object> GetInventoryStatsAsync(string search = "", string storeCode = "", bool? lowStock = null);
}