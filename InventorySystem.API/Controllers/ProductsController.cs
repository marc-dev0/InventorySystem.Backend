using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InventorySystem.Application.Interfaces;
using InventorySystem.Application.DTOs;
using InventorySystem.API.Controllers.Base;
using InventorySystem.Application.Services;

namespace InventorySystem.API.Controllers;

[Authorize(Policy = "UserOrAdmin")]
[ApiController]
[Route("api/[controller]")]
public class ProductsController : BaseSearchController<ProductDto>
{
    private readonly IProductService _productService;
    private readonly IImportLockService _importLockService;

    public ProductsController(IProductService productService, IImportLockService importLockService)
    {
        _productService = productService;
        _importLockService = importLockService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll()
    {
        var products = await _productService.GetAllAsync();
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetById(int id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product == null)
            return NotFound();

        return Ok(product);
    }

    [HttpGet("code/{code}")]
    public async Task<ActionResult<ProductDto>> GetByCode(string code)
    {
        var product = await _productService.GetByCodeAsync(code);
        if (product == null)
            return NotFound();

        return Ok(product);
    }

    [HttpGet("category/{categoryId}")]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetByCategory(int categoryId)
    {
        var products = await _productService.GetByCategoryAsync(categoryId);
        return Ok(products);
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetLowStock()
    {
        var products = await _productService.GetLowStockProductsAsync();
        return Ok(products);
    }

    protected override async Task<IEnumerable<ProductDto>> SearchItemsAsync(string term)
    {
        return await _productService.SearchProductsAsync(term);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var product = await _productService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            await _productService.UpdateAsync(id, dto);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("{id}/stock")]
    public async Task<IActionResult> UpdateStock(int id, [FromBody] UpdateStockDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            await _productService.UpdateStockAsync(id, dto.NewStock, dto.Reason);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        // Verificar si se puede eliminar el producto (no hay cargas activas)
        var canDelete = await _importLockService.CanDeleteAsync("PRODUCT", id);
        if (!canDelete)
        {
            return Conflict(new 
            { 
                error = "No se puede eliminar el producto en este momento",
                reason = "Hay cargas de datos en proceso que utilizan productos",
                suggestion = "Espere a que terminen las cargas activas antes de eliminar productos",
                checkStatusEndpoint = "/api/backgroundjobs/status/imports"
            });
        }

        try
        {
            await _productService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}