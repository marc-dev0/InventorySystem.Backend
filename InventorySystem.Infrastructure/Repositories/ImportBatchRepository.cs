using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using InventorySystem.Core.Entities;
using InventorySystem.Core.Interfaces;
using InventorySystem.Infrastructure.Data;

namespace InventorySystem.Infrastructure.Repositories;

public class ImportBatchRepository : Repository<ImportBatch>, IImportBatchRepository
{
    public ImportBatchRepository(InventoryDbContext context, ILogger<Repository<ImportBatch>> logger) : base(context, logger)
    {
    }

    public async Task<IEnumerable<ImportBatch>> GetActiveBatchesAsync()
    {
        return await _context.Set<ImportBatch>()
            .Where(b => !b.IsDeleted)
            .OrderByDescending(b => b.ImportDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<ImportBatch>> GetBatchesByTypeAsync(string batchType)
    {
        return await _context.Set<ImportBatch>()
            .Where(b => !b.IsDeleted && b.BatchType == batchType)
            .OrderByDescending(b => b.ImportDate)
            .ToListAsync();
    }

    public async Task<ImportBatch?> GetBatchWithDetailsAsync(int batchId)
    {
        return await _context.Set<ImportBatch>()
            .Include(b => b.Sales)
            .Include(b => b.Products)
            .Include(b => b.ProductStocks)
            .FirstOrDefaultAsync(b => b.Id == batchId);
    }

    public async Task<ImportBatch?> GetBatchByCodeAsync(string batchCode)
    {
        return await _context.Set<ImportBatch>()
            .FirstOrDefaultAsync(b => b.BatchCode == batchCode);
    }

    public async Task SoftDeleteAsync(int batchId, string deletedBy, string reason)
    {
        var batch = await GetByIdAsync(batchId);
        if (batch != null)
        {
            batch.IsDeleted = true;
            batch.DeletedAt = DateTime.UtcNow;
            batch.DeletedBy = deletedBy;
            batch.DeleteReason = reason;
            
            await UpdateAsync(batch);
        }
    }
}