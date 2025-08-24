using InventorySystem.Core.Entities;

namespace InventorySystem.Core.Interfaces;

public interface IBrandRepository
{
    Task<IEnumerable<Brand>> GetAllAsync();
    Task<Brand?> GetByIdAsync(int id);
    Task<Brand?> GetByNameAsync(string name);
    Task<Brand> AddAsync(Brand brand);
    Task<Brand> UpdateAsync(Brand brand);
    Task DeleteAsync(int id);
}