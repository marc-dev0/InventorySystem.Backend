using InventorySystem.Core.Entities;
using System.Linq.Expressions;

namespace InventorySystem.Core.Interfaces;

public interface IBackgroundJobRepository : IRepository<BackgroundJob>
{
    Task<BackgroundJob?> GetByJobIdAsync(string jobId);
    Task<List<BackgroundJob>> GetJobsByStatusAsync(string status);
    Task<List<BackgroundJob>> GetJobsByUserAsync(string userId);
    Task<List<BackgroundJob>> GetRecentJobsAsync(int count = 10);
    Task UpdateProgressAsync(string jobId, int processedRecords, decimal progressPercentage);
    Task UpdateStatusAsync(string jobId, string status, string? errorMessage = null);
    Task AddErrorAsync(string jobId, string error);
    Task AddWarningAsync(string jobId, string warning);
    
    // Métodos atómicos para evitar race conditions
    Task<(bool Success, string? ErrorMessage, string? JobId)> TryCreateJobAtomicallyAsync(BackgroundJob newJob);
    Task<bool> HasActiveJobsAsync();
    Task<BackgroundJob?> GetActiveJobAsync();
}