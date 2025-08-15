using Microsoft.AspNetCore.Mvc;
using InventorySystem.Application.Interfaces;
using InventorySystem.Application.DTOs;
using InventorySystem.API.Controllers.Base;

namespace InventorySystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PurchasesController : BaseReportController<PurchaseDto>
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

    protected override async Task<IEnumerable<PurchaseDto>> GetItemsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _purchaseService.GetPurchasesByDateRangeAsync(startDate, endDate);
    }

    protected override async Task<object> GetReportAsync(DateTime startDate, DateTime endDate)
    {
        return await _purchaseService.GetPurchasesReportAsync(startDate, endDate);
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