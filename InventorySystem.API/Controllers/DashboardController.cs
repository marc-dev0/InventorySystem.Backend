using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventorySystem.Infrastructure.Data;

namespace InventorySystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly InventoryDbContext _context;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(InventoryDbContext context, ILogger<DashboardController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats()
    {
        try
        {
            var stats = new DashboardStatsDto
            {
                TotalProducts = await _context.Products.CountAsync(),
                TotalStores = await _context.Stores.CountAsync(),
                TotalCategories = await _context.Categories.CountAsync(),
                TotalCustomers = await _context.Customers.CountAsync(),
                TotalBrands = await _context.Brands.CountAsync()
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard stats");
            return StatusCode(500, "Error interno del servidor");
        }
    }
}

public class DashboardStatsDto
{
    public int TotalProducts { get; set; }
    public int TotalStores { get; set; }
    public int TotalCategories { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalBrands { get; set; }
}