using InventorySystem.Application.DTOs;

namespace InventorySystem.Application.Interfaces;

public interface IStockValidationService
{
    /// <summary>
    /// Validates sales before processing to detect potential negative stock issues
    /// </summary>
    Task<StockValidationResultDto> ValidateSalesStockImpactAsync(List<TandiaSaleDetailDto> salesData, string storeCode);
    
    /// <summary>
    /// Validates current stock levels for a store
    /// </summary>
    Task<StockValidationResultDto> ValidateCurrentStockLevelsAsync(string storeCode);
    
    /// <summary>
    /// Validates if a single sale would cause negative stock
    /// </summary>
    Task<bool> ValidateSingleSaleStockAsync(string productCode, int quantity, string storeCode);
    
    /// <summary>
    /// Gets current stock for a product in a store
    /// </summary>
    Task<int> GetCurrentStockAsync(string productCode, string storeCode);
    
    /// <summary>
    /// Simulates stock changes to predict issues
    /// </summary>
    Task<List<StockIssueDto>> SimulateStockChangesAsync(List<TandiaSaleDetailDto> salesData, string storeCode);
}