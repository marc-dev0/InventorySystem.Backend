using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InventorySystem.Application.Interfaces;
using InventorySystem.Application.DTOs;
using InventorySystem.API.Controllers.Base;

namespace InventorySystem.API.Controllers;

[Authorize(Policy = "UserOrAdmin")]
public class CategoriesController : BaseCrudController<CategoryDto, CreateCategoryDto, UpdateCategoryDto>
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    // Implementation of abstract methods from BaseCrudController
    protected override async Task<IEnumerable<CategoryDto>> GetAllItemsAsync()
    {
        return await _categoryService.GetAllAsync();
    }

    protected override async Task<CategoryDto?> GetItemByIdAsync(int id)
    {
        return await _categoryService.GetByIdAsync(id);
    }

    protected override async Task<IEnumerable<CategoryDto>> GetActiveItemsAsync()
    {
        return await _categoryService.GetActiveCategoriesAsync();
    }

    protected override async Task<CategoryDto> CreateItemAsync(CreateCategoryDto createDto)
    {
        return await _categoryService.CreateAsync(createDto);
    }

    protected override async Task UpdateItemAsync(int id, UpdateCategoryDto updateDto)
    {
        await _categoryService.UpdateAsync(id, updateDto);
    }

    protected override async Task DeleteItemAsync(int id)
    {
        await _categoryService.DeleteAsync(id);
    }

    protected override int GetItemId(CategoryDto item)
    {
        return item.Id;
    }
}