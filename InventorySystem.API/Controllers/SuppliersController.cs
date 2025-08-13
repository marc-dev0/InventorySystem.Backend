using Microsoft.AspNetCore.Mvc;
using InventorySystem.Application.Interfaces;
using InventorySystem.Application.DTOs;

namespace InventorySystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _supplierService;

    public SuppliersController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SupplierDto>>> GetAll()
    {
        var suppliers = await _supplierService.GetAllAsync();
        return Ok(suppliers);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SupplierDto>> GetById(int id)
    {
        var supplier = await _supplierService.GetByIdAsync(id);
        if (supplier == null)
            return NotFound();

        return Ok(supplier);
    }

    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<SupplierDto>>> GetActive()
    {
        var suppliers = await _supplierService.GetActiveSuppliersAsync();
        return Ok(suppliers);
    }

    [HttpPost]
    public async Task<ActionResult<SupplierDto>> Create([FromBody] CreateSupplierDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var supplier = await _supplierService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = supplier.Id }, supplier);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSupplierDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            await _supplierService.UpdateAsync(id, dto);
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
        try
        {
            await _supplierService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}