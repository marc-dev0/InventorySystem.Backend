using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InventorySystem.Application.Interfaces;
using InventorySystem.Application.DTOs;
using InventorySystem.API.Controllers.Base;

namespace InventorySystem.API.Controllers;

[Authorize(Policy = "UserOrAdmin")]
public class SuppliersController : BaseCrudController<SupplierDto, CreateSupplierDto, UpdateSupplierDto>
{
    private readonly ISupplierService _supplierService;

    public SuppliersController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    // Implementation of abstract methods from BaseCrudController
    protected override async Task<IEnumerable<SupplierDto>> GetAllItemsAsync()
    {
        return await _supplierService.GetAllAsync();
    }

    protected override async Task<SupplierDto?> GetItemByIdAsync(int id)
    {
        return await _supplierService.GetByIdAsync(id);
    }

    protected override async Task<IEnumerable<SupplierDto>> GetActiveItemsAsync()
    {
        return await _supplierService.GetActiveSuppliersAsync();
    }

    protected override async Task<SupplierDto> CreateItemAsync(CreateSupplierDto createDto)
    {
        return await _supplierService.CreateAsync(createDto);
    }

    protected override async Task UpdateItemAsync(int id, UpdateSupplierDto updateDto)
    {
        await _supplierService.UpdateAsync(id, updateDto);
    }

    protected override async Task DeleteItemAsync(int id)
    {
        await _supplierService.DeleteAsync(id);
    }

    protected override int GetItemId(SupplierDto item)
    {
        return item.Id;
    }
}