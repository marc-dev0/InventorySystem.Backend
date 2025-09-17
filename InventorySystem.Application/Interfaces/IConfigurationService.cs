namespace InventorySystem.Application.Interfaces
{
    public interface IConfigurationService
    {
        Task<decimal> GetGlobalMinimumStockAsync();
        Task<bool> IsLowStockAsync(decimal currentStock, decimal productMinimumStock);
    }
}