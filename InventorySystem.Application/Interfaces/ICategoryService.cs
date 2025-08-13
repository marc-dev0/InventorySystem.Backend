using InventorySystem.Application.DTOs;

namespace InventorySystem.Application.Interfaces;

public interface ICategoryService
{
    Task<IEnumerable<CategoryDto>> GetAllAsync();
    Task<CategoryDto?> GetByIdAsync(int id);
    Task<IEnumerable<CategoryDto>> GetActiveCategoriesAsync();
    Task<CategoryDto> CreateAsync(CreateCategoryDto dto);
    Task UpdateAsync(int id, UpdateCategoryDto dto);
    Task DeleteAsync(int id);
}
