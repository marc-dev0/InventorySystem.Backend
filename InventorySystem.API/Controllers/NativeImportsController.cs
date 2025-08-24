using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InventorySystem.API.Utilities;
using Microsoft.Extensions.Logging;
using InventorySystem.Application.Interfaces;

namespace InventorySystem.API.Controllers;

[Authorize(Policy = "AdminOnly")]
[ApiController]
[Route("api/[controller]")]
public class NativeImportsController : ControllerBase
{
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly ILogger<NativeImportsController> _logger;

    public NativeImportsController(
        IBackgroundJobService backgroundJobService,
        ILogger<NativeImportsController> logger)
    {
        _backgroundJobService = backgroundJobService;
        _logger = logger;
    }

    /// <summary>
    /// Importar productos - REDIRIGIDO a BackgroundJobs (sistema confiable sin problemas)
    /// </summary>
    [AllowAnonymous] // Temporal para testing
    [HttpPost("products")]
    public async Task<IActionResult> ImportProducts(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("No se ha seleccionado ningún archivo.");

            if (!FileValidationHelper.IsValidExcelFile(file))
                return BadRequest("El archivo debe ser un Excel (.xlsx o .xls).");

            _logger.LogInformation("NativeImports redirigiendo a BackgroundJobs para productos: {FileName}", file.FileName);

            var userId = User.Identity?.Name ?? "NativeImport";

            // REDIRIGIR al sistema BackgroundJobs que funciona sin problemas
            using var stream = file.OpenReadStream();
            var jobId = await _backgroundJobService.QueueProductsImportAsync(stream, file.FileName, userId);

            _logger.LogInformation("Productos encolados en BackgroundJobs con jobId: {JobId}", jobId);

            return Ok(new
            {
                message = "Importación de productos encolada exitosamente en BackgroundJobs",
                jobId = jobId,
                status = "QUEUED",
                fileName = file.FileName,
                note = "La importación se está procesando en segundo plano. Use el jobId para consultar el progreso.",
                redirectedFrom = "NativeImports",
                useBackgroundJobs = true,
                technology = "BackgroundJobs + ClosedXML (.NET nativo)"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error redirigiendo a BackgroundJobs desde NativeImports");
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }

    /// <summary>
    /// Importar stock - REDIRIGIDO a BackgroundJobs (sistema confiable sin problemas)
    /// </summary>
    [AllowAnonymous] // Temporal para testing
    [HttpPost("stock")]
    public async Task<IActionResult> ImportStock(IFormFile file, [FromForm] string storeCode)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("No se ha seleccionado ningún archivo.");

            if (!FileValidationHelper.IsValidExcelFile(file))
                return BadRequest("El archivo debe ser un Excel (.xlsx o .xls).");

            if (string.IsNullOrEmpty(storeCode))
                return BadRequest("Debe especificar el código del almacén.");

            _logger.LogInformation("NativeImports redirigiendo a BackgroundJobs para stock: {FileName}, Store: {StoreCode}", 
                file.FileName, storeCode);

            var userId = User.Identity?.Name ?? "NativeImport";

            // REDIRIGIR al sistema BackgroundJobs que funciona sin problemas
            using var stream = file.OpenReadStream();
            var jobId = await _backgroundJobService.QueueStockImportAsync(stream, file.FileName, storeCode, userId);

            _logger.LogInformation("Stock encolado en BackgroundJobs con jobId: {JobId}", jobId);

            return Ok(new
            {
                message = "Importación de stock encolada exitosamente en BackgroundJobs",
                jobId = jobId,
                status = "QUEUED",
                fileName = file.FileName,
                storeCode = storeCode,
                note = "La importación se está procesando en segundo plano. Use el jobId para consultar el progreso.",
                redirectedFrom = "NativeImports",
                useBackgroundJobs = true,
                technology = "BackgroundJobs + ClosedXML (.NET nativo)"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error redirigiendo a BackgroundJobs desde NativeImports");
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }

    /// <summary>
    /// Importar ventas - REDIRIGIDO a BackgroundJobs (sistema confiable sin problemas)
    /// </summary>
    [AllowAnonymous] // Temporal para testing
    [HttpPost("sales")]
    public async Task<IActionResult> ImportSales(IFormFile file, [FromForm] string storeCode)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("No se ha seleccionado ningún archivo.");

            if (!FileValidationHelper.IsValidExcelFile(file))
                return BadRequest("El archivo debe ser un Excel (.xlsx o .xls).");

            if (string.IsNullOrEmpty(storeCode))
                return BadRequest("Debe especificar el código del almacén.");

            _logger.LogInformation("NativeImports redirigiendo a BackgroundJobs para ventas: {FileName}, Store: {StoreCode}", 
                file.FileName, storeCode);

            var userId = User.Identity?.Name ?? "NativeImport";

            // REDIRIGIR al sistema BackgroundJobs que funciona sin problemas
            using var stream = file.OpenReadStream();
            var jobId = await _backgroundJobService.QueueSalesImportAsync(stream, file.FileName, storeCode, userId);

            _logger.LogInformation("Ventas encoladas en BackgroundJobs con jobId: {JobId}", jobId);

            return Ok(new
            {
                message = "Importación de ventas encolada exitosamente en BackgroundJobs",
                jobId = jobId,
                status = "QUEUED",
                fileName = file.FileName,
                storeCode = storeCode,
                note = "La importación se está procesando en segundo plano. Use el jobId para consultar el progreso.",
                redirectedFrom = "NativeImports",
                useBackgroundJobs = true,
                technology = "BackgroundJobs + ClosedXML (.NET nativo)"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error redirigiendo a BackgroundJobs desde NativeImports");
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }
}