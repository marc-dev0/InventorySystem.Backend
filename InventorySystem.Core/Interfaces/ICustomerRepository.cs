using InventorySystem.Core.Entities;

namespace InventorySystem.Core.Interfaces;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<IEnumerable<Customer>> GetActiveCustomersAsync();
    Task<Customer?> GetByDocumentAsync(string document);
    Task<IEnumerable<Customer>> SearchCustomersAsync(string searchTerm);
}
