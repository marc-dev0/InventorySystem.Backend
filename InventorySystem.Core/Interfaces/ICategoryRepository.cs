using InventorySystem.Core.Entities;

namespace InventorySystem.Core.Interfaces;

public interface ICategoryRepository : IRepository<Category>
{
    Task<IEnumerable<Category>> GetActiveCategoriesAsync();
    Task<bool> HasProductsAsync(int categoryId);
    Task<Category?> GetByNameAsync(string name);
}
