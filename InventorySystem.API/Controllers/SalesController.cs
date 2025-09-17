using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InventorySystem.Application.Interfaces;
using InventorySystem.Application.DTOs;
using InventorySystem.API.Controllers.Base;

namespace InventorySystem.API.Controllers;

[Authorize(Policy = "UserOrAdmin")]
[ApiController]
[Route("api/[controller]")]
public class SalesController : BaseReportController<SaleDto>
{
    private readonly ISaleService _saleService;

    public SalesController(ISaleService saleService)
    {
        _saleService = saleService;
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string search = "",
        [FromQuery] string storeCode = "")
    {
        try
        {
            // Validate parameters
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var result = await _saleService.GetPaginatedAsync(page, pageSize, search, storeCode);

            // Add statistics to the response like in ProductsController and InventoryController
            var stats = await _saleService.GetSalesStatsAsync(search, storeCode);
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
    public async Task<ActionResult<IEnumerable<SaleDto>>> GetAllSales()
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

    protected override async Task<IEnumerable<SaleDto>> GetItemsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _saleService.GetSalesByDateRangeAsync(startDate, endDate);
    }

    protected override async Task<object> GetReportAsync(DateTime startDate, DateTime endDate)
    {
        return await _saleService.GetSalesReportAsync(startDate, endDate);
    }

    [HttpGet("customer/{customerId}")]
    public async Task<ActionResult<IEnumerable<SaleDto>>> GetByCustomer(int customerId)
    {
        var sales = await _saleService.GetSalesByCustomerAsync(customerId);
        return Ok(sales);
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