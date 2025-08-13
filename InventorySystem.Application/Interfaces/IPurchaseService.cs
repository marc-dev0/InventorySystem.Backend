using InventorySystem.Application.DTOs;

namespace InventorySystem.Application.Interfaces;

public interface IPurchaseService
{
    Task<IEnumerable<PurchaseDto>> GetAllAsync();
    Task<PurchaseDto?> GetByIdAsync(int id);
    Task<PurchaseDetailsDto?> GetPurchaseDetailsAsync(int id);
    Task<IEnumerable<PurchaseDto>> GetPurchasesByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<PurchaseDto> CreateAsync(CreatePurchaseDto dto);
    Task<object> GetPurchasesReportAsync(DateTime startDate, DateTime endDate);
}