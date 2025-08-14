using InventorySystem.Application.DTOs;

namespace InventorySystem.Application.Interfaces;

public interface ISalesImportTrackingService
{
    Task<List<ImportBatchDto>> GetRecentImportsAsync(int days = 30);
    Task<SalesDeleteResultDto> DeleteSalesImportAsync(string batchCode, string deletedBy);
    Task<ImportBatchDto?> GetBatchByCodeAsync(string batchCode);
}