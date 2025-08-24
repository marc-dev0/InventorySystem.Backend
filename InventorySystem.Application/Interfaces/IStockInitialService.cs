using InventorySystem.Application.DTOs;

namespace InventorySystem.Application.Interfaces;

public interface IStockInitialService
{
    Task<StockLoadResultDto> LoadStockFromExcelAsync(Stream excelStream, string fileName, string storeCode);
    Task<StockClearResultDto> ClearAllStockAsync();
    Task<StoreClearResultDto> ClearStockByStoreAsync(string storeCode);
    Task<StockSummaryDto> GetStockSummaryAsync();
    Task<int> DeleteProductStocksByBatchIdAsync(int batchId);
}