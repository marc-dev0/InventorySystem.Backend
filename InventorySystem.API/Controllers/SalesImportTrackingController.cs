using Microsoft.AspNetCore.Mvc;
using InventorySystem.Application.Interfaces;

namespace InventorySystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalesImportTrackingController : ControllerBase
{
    private readonly ISalesImportTrackingService _salesImportTrackingService;
    private readonly ILogger<SalesImportTrackingController> _logger;

    public SalesImportTrackingController(
        ISalesImportTrackingService salesImportTrackingService,
        ILogger<SalesImportTrackingController> logger)
    {
        _salesImportTrackingService = salesImportTrackingService;
        _logger = logger;
    }

    /// <summary>
    /// Obtener cargas recientes de ventas (últimos 30 días)
    /// </summary>
    [HttpGet("recent-imports")]
    public async Task<IActionResult> GetRecentImports([FromQuery] int days = 30)
    {
        try
        {
            if (days < 1 || days > 365)
            {
                return BadRequest("Los días deben estar entre 1 y 365");
            }

            var imports = await _salesImportTrackingService.GetRecentImportsAsync(days);
            
            return Ok(new
            {
                message = $"Cargas de ventas de los últimos {days} días",
                totalBatches = imports.Count,
                imports = imports
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener cargas recientes");
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }

    /// <summary>
    /// Eliminar carga específica de ventas y revertir stock por código de lote
    /// </summary>
    [HttpDelete("delete-import/{batchCode}")]
    public async Task<IActionResult> DeleteSalesImport(
        string batchCode,
        [FromBody] DeleteImportByCodeRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(batchCode))
            {
                return BadRequest("BatchCode es requerido");
            }

            if (string.IsNullOrEmpty(request.DeletedBy))
            {
                return BadRequest("DeletedBy es requerido");
            }

            _logger.LogWarning("Iniciando eliminación de carga de ventas. Código: {BatchCode}, Usuario: {DeletedBy}", 
                batchCode, request.DeletedBy);

            var result = await _salesImportTrackingService.DeleteSalesImportAsync(
                batchCode, 
                request.DeletedBy);

            if (result.DeletedSales == 0)
            {
                return NotFound("No se encontraron ventas para eliminar con el código especificado");
            }

            _logger.LogWarning("Carga eliminada exitosamente. Ventas: {DeletedSales}, Movimientos revertidos: {RevertedMovements}, Productos afectados: {AffectedProducts}", 
                result.DeletedSales, result.RevertedMovements, result.AffectedProducts);

            return Ok(new 
            { 
                message = "Carga de ventas eliminada exitosamente",
                result = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar carga de ventas");
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtener detalles de una carga específica por código de lote
    /// </summary>
    [HttpGet("batch/{batchCode}")]
    public async Task<IActionResult> GetBatchDetails(string batchCode)
    {
        try
        {
            if (string.IsNullOrEmpty(batchCode))
            {
                return BadRequest("BatchCode es requerido");
            }

            var batch = await _salesImportTrackingService.GetBatchByCodeAsync(batchCode);

            if (batch == null)
            {
                return NotFound("No se encontró el lote especificado");
            }

            return Ok(batch);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener detalles del lote");
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }
}

public class DeleteImportRequest
{
    public DateTime ImportedAt { get; set; }
    public string ImportSource { get; set; } = string.Empty;
    public string DeletedBy { get; set; } = string.Empty;
}

public class DeleteImportByCodeRequest
{
    public string DeletedBy { get; set; } = string.Empty;
    public string? DeleteReason { get; set; }
}