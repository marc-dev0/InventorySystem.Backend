using InventorySystem.Core.Entities;

namespace InventorySystem.Core.Interfaces;

public interface IImportBatchRepository : IRepository<ImportBatch>
{
    Task<IEnumerable<ImportBatch>> GetActiveBatchesAsync();
    Task<IEnumerable<ImportBatch>> GetBatchesByTypeAsync(string batchType);
    Task<ImportBatch?> GetBatchWithDetailsAsync(int batchId);
    Task<ImportBatch?> GetBatchByCodeAsync(string batchCode);
    Task SoftDeleteAsync(int batchId, string deletedBy, string reason);
}