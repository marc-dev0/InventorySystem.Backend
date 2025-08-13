using Microsoft.AspNetCore.Mvc;
using InventorySystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventorySystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly InventoryDbContext _context;

    public TestController(InventoryDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            // Test database connection
            var canConnect = await _context.Database.CanConnectAsync();
            
            var result = new
            {
                message = "¡API funcionando correctamente!",
                timestamp = DateTime.Now,
                database_connected = canConnect,
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.Now });
    }
}