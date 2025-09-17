using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InventorySystem.Application.Interfaces;
using InventorySystem.Application.DTOs;

namespace InventorySystem.API.Controllers;

[Authorize(Policy = "UserOrAdmin")]
[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string search = "",
        [FromQuery] string storeCode = "",
        [FromQuery] bool? lowStock = null)
    {
        try
        {
            // Validate parameters
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var result = await _inventoryService.GetPaginatedAsync(page, pageSize, search, storeCode, lowStock);

            // Add statistics to the response like in ProductsController
            var stats = await _inventoryService.GetInventoryStatsAsync(search, storeCode, lowStock);
            var response = new
            {
                Data = result.Data,
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize,
                TotalPages = result.TotalPages,
                Stats = stats
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = "Error interno del servidor", Details = ex.Message });
        }
    }

    [HttpGet("all")]
    public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetAllItems()
    {
        var items = await _inventoryService.GetAllAsync();
        return Ok(items);
    }

    [HttpGet("store/{storeId}")]
    public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetByStore(int storeId)
    {
        var items = await _inventoryService.GetByStoreAsync(storeId);
        return Ok(items);
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetLowStock()
    {
        var items = await _inventoryService.GetLowStockItemsAsync();
        return Ok(items);
    }
}