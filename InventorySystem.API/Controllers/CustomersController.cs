using Microsoft.AspNetCore.Mvc;
using InventorySystem.Application.Interfaces;
using InventorySystem.Application.DTOs;

namespace InventorySystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetAll()
    {
        var customers = await _customerService.GetAllAsync();
        return Ok(customers);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerDto>> GetById(int id)
    {
        var customer = await _customerService.GetByIdAsync(id);
        if (customer == null)
            return NotFound();

        return Ok(customer);
    }

    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetActive()
    {
        var customers = await _customerService.GetActiveCustomersAsync();
        return Ok(customers);
    }

    [HttpGet("document/{document}")]
    public async Task<ActionResult<CustomerDto>> GetByDocument(string document)
    {
        var customer = await _customerService.GetByDocumentAsync(document);
        if (customer == null)
            return NotFound();

        return Ok(customer);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> Search([FromQuery] string term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return BadRequest("Search term is required");

        var customers = await _customerService.SearchCustomersAsync(term);
        return Ok(customers);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create([FromBody] CreateCustomerDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var customer = await _customerService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            await _customerService.UpdateAsync(id, dto);
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
            await _customerService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}