using InventorySystem.Application.DTOs;

namespace InventorySystem.Application.Interfaces;

public interface ISaleService
{
    Task<IEnumerable<SaleDto>> GetAllAsync();
    Task<SaleDto?> GetByIdAsync(int id);
    Task<SaleDetailsDto?> GetSaleDetailsAsync(int id);
    Task<IEnumerable<SaleDto>> GetSalesByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<SaleDto>> GetSalesByCustomerAsync(int customerId);
    Task<SaleDto> CreateAsync(CreateSaleDto dto);
    Task<object> GetSalesReportAsync(DateTime startDate, DateTime endDate);
    Task<object> GetSalesDashboardAsync();
}
