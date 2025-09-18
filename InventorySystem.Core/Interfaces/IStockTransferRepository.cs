using InventorySystem.Core.Entities;

namespace InventorySystem.Core.Interfaces;

public interface IStockTransferRepository : IRepository<StockTransfer>
{
    Task<IEnumerable<StockTransfer>> GetTransfersByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<StockTransfer?> GetTransferDetailsAsync(int transferId);
    Task<string> GenerateTransferNumberAsync();
    Task<IEnumerable<StockTransfer>> GetTransfersByOriginStoreAsync(int originStoreId);
    Task<IEnumerable<StockTransfer>> GetTransfersByDestinationStoreAsync(int destinationStoreId);
    Task<IEnumerable<StockTransfer>> GetPendingTransfersAsync();
    Task<IEnumerable<StockTransfer>> GetTransfersByStatusAsync(TransferStatus status);
}