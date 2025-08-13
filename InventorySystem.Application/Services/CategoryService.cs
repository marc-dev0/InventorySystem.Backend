using InventorySystem.Application.DTOs;
using InventorySystem.Application.Interfaces;
using InventorySystem.Core.Entities;
using InventorySystem.Core.Interfaces;

namespace InventorySystem.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<IEnumerable<CategoryDto>> GetAllAsync()
    {
        var categories = await _categoryRepository.GetAllAsync();
        return categories.Select(MapToDto);
    }

    public async Task<CategoryDto?> GetByIdAsync(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        return category != null ? MapToDto(category) : null;
    }

    public async Task<IEnumerable<CategoryDto>> GetActiveCategoriesAsync()
    {
        var categories = await _categoryRepository.GetActiveCategoriesAsync();
        return categories.Select(MapToDto);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto)
    {
        // Check if category with same name exists
        var existingCategory = await _categoryRepository.GetByNameAsync(dto.Name);
        if (existingCategory != null)
        {
            throw new ArgumentException($"Category with name '{dto.Name}' already exists");
        }

        var category = new Category
        {
            Name = dto.Name,
            Description = dto.Description,
            Active = true
        };

        var createdCategory = await _categoryRepository.AddAsync(category);
        return MapToDto(createdCategory);
    }

    public async Task UpdateAsync(int id, UpdateCategoryDto dto)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null)
        {
            throw new KeyNotFoundException("Category not found");
        }

        // Check if another category with same name exists
        var existingCategory = await _categoryRepository.GetByNameAsync(dto.Name);
        if (existingCategory != null && existingCategory.Id != id)
        {
            throw new ArgumentException($"Another category with name '{dto.Name}' already exists");
        }

        category.Name = dto.Name;
        category.Description = dto.Description;
        category.Active = dto.Active;

        await _categoryRepository.UpdateAsync(category);
    }

    public async Task DeleteAsync(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null)
        {
            throw new KeyNotFoundException("Category not found");
        }

        // Check if category has products
        if (await _categoryRepository.HasProductsAsync(id))
        {
            throw new InvalidOperationException("Cannot delete category that has associated products");
        }

        category.IsDeleted = true;
        await _categoryRepository.UpdateAsync(category);
    }

    private static CategoryDto MapToDto(Category category)
    {
        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            Active = category.Active,
            ProductCount = category.Products?.Count ?? 0,
            CreatedAt = category.CreatedAt
        };
    }
}
