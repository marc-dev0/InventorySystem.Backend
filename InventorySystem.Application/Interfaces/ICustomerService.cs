using InventorySystem.Application.DTOs;

namespace InventorySystem.Application.Interfaces;

public interface ICustomerService
{
    Task<IEnumerable<CustomerDto>> GetAllAsync();
    Task<CustomerDto?> GetByIdAsync(int id);
    Task<IEnumerable<CustomerDto>> GetActiveCustomersAsync();
    Task<CustomerDto?> GetByDocumentAsync(string document);
    Task<IEnumerable<CustomerDto>> SearchCustomersAsync(string searchTerm);
    Task<CustomerDto> CreateAsync(CreateCustomerDto dto);
    Task UpdateAsync(int id, UpdateCustomerDto dto);
    Task DeleteAsync(int id);
}
