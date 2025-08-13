using Microsoft.AspNetCore.Mvc;

namespace InventorySystem.API.Controllers;

[Route("")]
public class HomeController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        var html = @"
<!DOCTYPE html>
<html>
<head>
    <title>Inventory System API</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 40px; background: #f5f5f5; }
        .container { background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
        h1 { color: #2c3e50; }
        .link { display: inline-block; margin: 10px 0; padding: 10px 15px; background: #3498db; color: white; text-decoration: none; border-radius: 5px; }
        .link:hover { background: #2980b9; }
    </style>
</head>
<body>
    <div class='container'>
        <h1>🏭 Inventory System API</h1>
        <p>¡Bienvenido al sistema de inventario! La API está funcionando correctamente.</p>
        
        <h3>Enlaces útiles:</h3>
        <a href='/swagger' class='link'>📖 Swagger Documentation</a><br>
        <a href='/api/test' class='link'>🧪 Test Endpoint</a><br>
        <a href='/api/test/health' class='link'>❤️ Health Check</a><br>
        
        <h3>Información:</h3>
        <ul>
            <li><strong>Timestamp:</strong> " + DateTime.Now + @"</li>
            <li><strong>Environment:</strong> " + Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") + @"</li>
            <li><strong>Version:</strong> 1.0.0</li>
        </ul>
    </div>
</body>
</html>";

        return Content(html, "text/html");
    }
}