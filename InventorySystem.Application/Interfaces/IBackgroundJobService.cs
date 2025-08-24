using InventorySystem.Application.DTOs;
using InventorySystem.Core.Entities;

namespace InventorySystem.Application.Interfaces;

public interface IBackgroundJobService
{
    Task<string> QueueSalesImportAsync(Stream excelStream, string fileName, string storeCode, string userId);
    Task<string> QueueStockImportAsync(Stream excelStream, string fileName, string storeCode, string userId);
    Task<string> QueueProductsImportAsync(Stream excelStream, string fileName, string userId);
    
    Task<Core.Entities.BackgroundJob?> GetJobStatusAsync(string jobId);
    Task<List<Core.Entities.BackgroundJob>> GetUserJobsAsync(string userId);
    Task<List<Core.Entities.BackgroundJob>> GetRecentJobsAsync(int count = 10);
    
    // Background job execution methods (called by Hangfire)
    Task ProcessSalesImportAsync(string jobId, byte[] fileData, string fileName, string storeCode);
    Task ProcessStockImportAsync(string jobId, byte[] fileData, string fileName, string storeCode);
    Task ProcessProductsImportAsync(string jobId, byte[] fileData, string fileName);
}