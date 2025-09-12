using InventorySystem.Core.Entities;

namespace InventorySystem.Core.Interfaces;

public interface IEmployeeRepository : IRepository<Employee>
{
    Task<Employee?> GetByCodeAsync(string code);
    Task<IEnumerable<Employee>> GetByStoreIdAsync(int storeId);
    Task<Employee?> GetByNameAsync(string name);
}