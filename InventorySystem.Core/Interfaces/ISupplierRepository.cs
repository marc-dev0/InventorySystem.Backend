using InventorySystem.Core.Entities;

namespace InventorySystem.Core.Interfaces;

public interface ISupplierRepository : IRepository<Supplier>
{
    Task<IEnumerable<Supplier>> GetActiveSuppliersAsync();
    Task<bool> HasProductsAsync(int supplierId);
    Task<Supplier?> GetByNameAsync(string name);
}
