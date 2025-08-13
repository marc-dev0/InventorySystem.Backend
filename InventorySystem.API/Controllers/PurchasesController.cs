using Microsoft.AspNetCore.Mvc;
using InventorySystem.Application.Interfaces;
using InventorySystem.Application.DTOs;

namespace InventorySystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PurchasesController : ControllerBase
{
    private readonly IPurchaseService _purchaseService;

    public PurchasesController(IPurchaseService purchaseService)
    {
        _purchaseService = purchaseService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PurchaseDto>>> GetAll()
    {
        var purchases = await _purchaseService.GetAllAsync();
        return Ok(purchases);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PurchaseDto>> GetById(int id)
    {
        var purchase = await _purchaseService.GetByIdAsync(id);
        if (purchase == null)
            return NotFound();

        return Ok(purchase);
    }

    [HttpGet("{id}/details")]
    public async Task<ActionResult<PurchaseDetailsDto>> GetDetails(int id)
    {
        var purchase = await _purchaseService.GetPurchaseDetailsAsync(id);
        if (purchase == null)
            return NotFound();

        return Ok(purchase);
    }

    [HttpGet("date-range")]
    public async Task<ActionResult<IEnumerable<PurchaseDto>>> GetByDateRange(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        if (startDate > endDate)
            return BadRequest("Start date cannot be greater than end date");

        var purchases = await _purchaseService.GetPurchasesByDateRangeAsync(startDate, endDate);
        return Ok(purchases);
    }

    [HttpGet("reports")]
    public async Task<ActionResult<object>> GetReport(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        if (startDate > endDate)
            return BadRequest("Start date cannot be greater than end date");

        var report = await _purchaseService.GetPurchasesReportAsync(startDate, endDate);
        return Ok(report);
    }

    [HttpPost]
    public async Task<ActionResult<PurchaseDto>> Create([FromBody] CreatePurchaseDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!dto.Details.Any())
            return BadRequest("Purchase must have at least one detail item");

        try
        {
            var purchase = await _purchaseService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = purchase.Id }, purchase);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}