using InventorySystem.Application.DTOs;
using InventorySystem.Core.Entities;

namespace InventorySystem.Application.Interfaces;

public interface IInventoryMovementService
{
    Task<InventoryMovementDto> CreateMovementAsync(CreateInventoryMovementDto dto);
    Task<IEnumerable<InventoryMovementDto>> GetMovementsByProductAsync(int productId);
    Task<IEnumerable<InventoryMovementDto>> GetMovementsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<InventoryMovementDto>> GetRecentMovementsAsync(int count = 50);
    Task<InventoryMovementSummaryDto> GetMovementsSummaryAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<ProductStockHistoryDto> GetProductStockHistoryAsync(int productId, DateTime? startDate = null, DateTime? endDate = null);
    Task RecordSaleMovementAsync(int productId, int quantity, string saleNumber, decimal unitPrice);
    Task RecordPurchaseMovementAsync(int productId, int quantity, string purchaseNumber, decimal unitCost);
    Task RecordTandiaImportMovementAsync(int productId, int oldStock, int newStock, string source);
    Task RecordManualAdjustmentAsync(int productId, int newStock, string reason, string userName);
}