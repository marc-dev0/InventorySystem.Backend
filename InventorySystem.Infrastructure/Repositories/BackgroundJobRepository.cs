using InventorySystem.Core.Entities;
using InventorySystem.Core.Interfaces;
using InventorySystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InventorySystem.Infrastructure.Repositories;

public class BackgroundJobRepository : Repository<BackgroundJob>, IBackgroundJobRepository
{
    public BackgroundJobRepository(InventoryDbContext context, ILogger<Repository<BackgroundJob>> logger) : base(context, logger)
    {
    }

    public async Task<BackgroundJob?> GetByJobIdAsync(string jobId)
    {
        return await _context.Set<BackgroundJob>()
            .Include(j => j.ImportBatch)
            .FirstOrDefaultAsync(j => j.JobId == jobId);
    }

    public async Task<List<BackgroundJob>> GetJobsByStatusAsync(string status)
    {
        return await _context.Set<BackgroundJob>()
            .Include(j => j.ImportBatch)
            .Where(j => j.Status == status)
            .OrderByDescending(j => j.StartedAt)
            .ToListAsync();
    }

    public async Task<List<BackgroundJob>> GetJobsByUserAsync(string userId)
    {
        return await _context.Set<BackgroundJob>()
            .Include(j => j.ImportBatch)
            .Where(j => j.StartedBy == userId)
            .OrderByDescending(j => j.StartedAt)
            .ToListAsync();
    }

    public async Task<List<BackgroundJob>> GetRecentJobsAsync(int count = 10)
    {
        return await _context.Set<BackgroundJob>()
            .Include(j => j.ImportBatch)
            .OrderByDescending(j => j.StartedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task UpdateProgressAsync(string jobId, int processedRecords, decimal progressPercentage)
    {
        var job = await GetByJobIdAsync(jobId);
        if (job != null)
        {
            job.ProcessedRecords = processedRecords;
            job.ProgressPercentage = progressPercentage;
            job.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateStatusAsync(string jobId, string status, string? errorMessage = null)
    {
        var job = await GetByJobIdAsync(jobId);
        if (job != null)
        {
            job.Status = status;
            job.UpdatedAt = DateTime.UtcNow;
            
            if (status == "COMPLETED" || status == "FAILED")
            {
                job.CompletedAt = DateTime.UtcNow;
            }
            
            if (!string.IsNullOrEmpty(errorMessage))
            {
                job.ErrorMessage = errorMessage;
            }
            
            await _context.SaveChangesAsync();
        }
    }

    public async Task AddErrorAsync(string jobId, string error)
    {
        var job = await GetByJobIdAsync(jobId);
        if (job != null)
        {
            job.DetailedErrors.Add(error);
            job.ErrorRecords++;
            job.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task AddWarningAsync(string jobId, string warning)
    {
        var job = await GetByJobIdAsync(jobId);
        if (job != null)
        {
            job.DetailedWarnings.Add(warning);
            job.WarningRecords++;
            job.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    // MÉTODO ATÓMICO: Verificar si hay jobs activos Y crear nuevo job en una sola transacción
    public async Task<(bool Success, string? ErrorMessage, string? JobId)> TryCreateJobAtomicallyAsync(BackgroundJob newJob)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // 1. Verificar jobs activos con lock exclusivo (FOR UPDATE en PostgreSQL)
            var activeStatuses = new[] { "PENDING", "PROCESSING", "QUEUED" };
            var activeJob = await _context.Set<BackgroundJob>()
                .Where(job => activeStatuses.Contains(job.Status))
                .FirstOrDefaultAsync();
            
            if (activeJob != null)
            {
                await transaction.RollbackAsync();
                var activeJobTypeDisplay = activeJob.JobType switch
                {
                    "SALES_IMPORT" => "carga de ventas",
                    "PRODUCTS_IMPORT" => "carga de productos", 
                    "STOCK_IMPORT" => "carga de stock inicial",
                    _ => "carga"
                };
                var storeInfo = !string.IsNullOrEmpty(activeJob.StoreCode) ? $" en tienda {activeJob.StoreCode}" : "";
                return (false, $"No se puede iniciar nueva carga: hay una {activeJobTypeDisplay}{storeInfo} en proceso (ID: {activeJob.JobId}). Solo se permite un proceso a la vez.", null);
            }
            
            // 2. Si no hay jobs activos, crear el nuevo job
            await _context.Set<BackgroundJob>().AddAsync(newJob);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            
            return (true, null, newJob.JobId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating background job atomically: {Message}", ex.Message);
            return (false, $"Error interno al crear job: {ex.Message}", null);
        }
    }

    // MÉTODO RÁPIDO: Solo verificar si hay jobs activos (sin crear)
    public async Task<bool> HasActiveJobsAsync()
    {
        var activeStatuses = new[] { "PENDING", "PROCESSING", "QUEUED" };
        return await _context.Set<BackgroundJob>()
            .AnyAsync(job => activeStatuses.Contains(job.Status));
    }

    // MÉTODO RÁPIDO: Obtener job activo actual (si existe)
    public async Task<BackgroundJob?> GetActiveJobAsync()
    {
        var activeStatuses = new[] { "PENDING", "PROCESSING", "QUEUED" };
        return await _context.Set<BackgroundJob>()
            .Where(job => activeStatuses.Contains(job.Status))
            .OrderBy(job => job.StartedAt) // El más antiguo primero
            .FirstOrDefaultAsync();
    }
}