using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InventorySystem.Application.Interfaces;
using InventorySystem.Application.DTOs;
using InventorySystem.API.Controllers.Base;

namespace InventorySystem.API.Controllers;

[Authorize(Policy = "UserOrAdmin")]
public class CustomersController : BaseCrudSearchController<CustomerDto, CreateCustomerDto, UpdateCustomerDto>
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    // Implementation of abstract methods from BaseCrudController
    protected override async Task<IEnumerable<CustomerDto>> GetAllItemsAsync()
    {
        return await _customerService.GetAllAsync();
    }

    protected override async Task<CustomerDto?> GetItemByIdAsync(int id)
    {
        return await _customerService.GetByIdAsync(id);
    }

    protected override async Task<IEnumerable<CustomerDto>> GetActiveItemsAsync()
    {
        return await _customerService.GetActiveCustomersAsync();
    }

    protected override async Task<CustomerDto> CreateItemAsync(CreateCustomerDto createDto)
    {
        return await _customerService.CreateAsync(createDto);
    }

    protected override async Task UpdateItemAsync(int id, UpdateCustomerDto updateDto)
    {
        await _customerService.UpdateAsync(id, updateDto);
    }

    protected override async Task DeleteItemAsync(int id)
    {
        await _customerService.DeleteAsync(id);
    }

    protected override async Task<IEnumerable<CustomerDto>> SearchItemsAsync(string term)
    {
        return await _customerService.SearchCustomersAsync(term);
    }

    protected override int GetItemId(CustomerDto item)
    {
        return item.Id;
    }

    // Additional endpoints unique to Customers
    [HttpGet("document/{document}")]
    public async Task<ActionResult<CustomerDto>> GetByDocument(string document)
    {
        try
        {
            var customer = await _customerService.GetByDocumentAsync(document);
            if (customer == null)
                return NotFound();

            return Ok(customer);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }
}