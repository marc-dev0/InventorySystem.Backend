using Microsoft.AspNetCore.Mvc;
using InventorySystem.Application.Interfaces;
using InventorySystem.Application.DTOs;

namespace InventorySystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalesController : ControllerBase
{
    private readonly ISaleService _saleService;

    public SalesController(ISaleService saleService)
    {
        _saleService = saleService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SaleDto>>> GetAll()
    {
        var sales = await _saleService.GetAllAsync();
        return Ok(sales);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SaleDto>> GetById(int id)
    {
        var sale = await _saleService.GetByIdAsync(id);
        if (sale == null)
            return NotFound();

        return Ok(sale);
    }

    [HttpGet("{id}/details")]
    public async Task<ActionResult<SaleDetailsDto>> GetDetails(int id)
    {
        var sale = await _saleService.GetSaleDetailsAsync(id);
        if (sale == null)
            return NotFound();

        return Ok(sale);
    }

    [HttpGet("date-range")]
    public async Task<ActionResult<IEnumerable<SaleDto>>> GetByDateRange(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        if (startDate > endDate)
            return BadRequest("Start date cannot be greater than end date");

        var sales = await _saleService.GetSalesByDateRangeAsync(startDate, endDate);
        return Ok(sales);
    }

    [HttpGet("customer/{customerId}")]
    public async Task<ActionResult<IEnumerable<SaleDto>>> GetByCustomer(int customerId)
    {
        var sales = await _saleService.GetSalesByCustomerAsync(customerId);
        return Ok(sales);
    }

    [HttpGet("reports")]
    public async Task<ActionResult<object>> GetReport(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        if (startDate > endDate)
            return BadRequest("Start date cannot be greater than end date");

        var report = await _saleService.GetSalesReportAsync(startDate, endDate);
        return Ok(report);
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<object>> GetDashboard()
    {
        var dashboard = await _saleService.GetSalesDashboardAsync();
        return Ok(dashboard);
    }

    [HttpPost]
    public async Task<ActionResult<SaleDto>> Create([FromBody] CreateSaleDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!dto.Details.Any())
            return BadRequest("Sale must have at least one detail item");

        try
        {
            var sale = await _saleService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = sale.Id }, sale);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}