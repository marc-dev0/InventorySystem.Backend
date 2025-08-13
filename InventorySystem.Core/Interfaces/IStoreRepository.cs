using InventorySystem.Core.Entities;

namespace InventorySystem.Core.Interfaces;

public interface IStoreRepository : IRepository<Store>
{
    Task<Store?> GetByNameAsync(string name);
    Task<Store?> GetByCodeAsync(string code);
    Task<IEnumerable<Store>> GetActiveStoresAsync();
}