using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InventorySystem.Core.Interfaces;
using InventorySystem.Application.DTOs;

namespace InventorySystem.API.Controllers;

[Authorize(Policy = "AdminOnly")]
[ApiController]
[Route("api/[controller]")]
public class ImportBatchesController : ControllerBase
{
    private readonly IImportBatchRepository _importBatchRepository;
    private readonly ILogger<ImportBatchesController> _logger;

    public ImportBatchesController(IImportBatchRepository importBatchRepository, ILogger<ImportBatchesController> logger)
    {
        _importBatchRepository = importBatchRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all import batches history
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetImportHistory()
    {
        try
        {
            var batches = await _importBatchRepository.GetAllAsync();
            
            var result = batches.OrderByDescending(b => b.ImportDate).Select(b => new
            {
                Id = b.Id,
                BatchCode = b.BatchCode,
                BatchType = b.BatchType,
                FileName = b.FileName,
                StoreCode = b.StoreCode,
                TotalRecords = b.TotalRecords,
                SuccessCount = b.SuccessCount,
                SkippedCount = b.SkippedCount,
                ErrorCount = b.ErrorCount,
                ImportDate = b.ImportDate,
                ImportedBy = b.ImportedBy,
                DeletedAt = b.DeletedAt,
                DeletedBy = b.DeletedBy,
                DeleteReason = b.DeleteReason
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting import history");
            return StatusCode(500, "Error interno del servidor");
        }
    }

    /// <summary>
    /// Get specific import batch by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetImportBatch(int id)
    {
        try
        {
            var batch = await _importBatchRepository.GetByIdAsync(id);
            if (batch == null)
                return NotFound();

            var result = new
            {
                Id = batch.Id,
                BatchCode = batch.BatchCode,
                BatchType = batch.BatchType,
                FileName = batch.FileName,
                StoreCode = batch.StoreCode,
                TotalRecords = batch.TotalRecords,
                SuccessCount = batch.SuccessCount,
                SkippedCount = batch.SkippedCount,
                ErrorCount = batch.ErrorCount,
                Errors = batch.Errors,
                Warnings = batch.Warnings,
                ImportDate = batch.ImportDate,
                ImportedBy = batch.ImportedBy,
                DeletedAt = batch.DeletedAt,
                DeletedBy = batch.DeletedBy,
                DeleteReason = batch.DeleteReason
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting import batch {ImportBatchId}", id);
            return StatusCode(500, "Error interno del servidor");
        }
    }

    /// <summary>
    /// Delete/Mark as deleted specific import batch and related data
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<object>> DeleteImportBatch(int id)
    {
        try
        {
            var batch = await _importBatchRepository.GetByIdAsync(id);
            if (batch == null)
                return NotFound("Importación no encontrada");

            if (batch.DeletedAt.HasValue)
                return BadRequest("Esta importación ya fue eliminada");

            // TODO: Implement logic to delete related data based on batch type
            // This would involve deleting products, sales, or stock based on ImportBatchId
            
            // For now, just mark the batch as deleted
            batch.DeletedAt = DateTime.UtcNow;
            batch.DeletedBy = User.Identity?.Name ?? "System";
            batch.DeleteReason = "Eliminado desde interfaz de administración";

            await _importBatchRepository.UpdateAsync(batch);

            var result = new
            {
                message = $"Importación {batch.FileName} marcada como eliminada exitosamente",
                batchCode = batch.BatchCode,
                fileName = batch.FileName,
                deletedAt = batch.DeletedAt
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting import batch {ImportBatchId}", id);
            return StatusCode(500, "Error interno del servidor");
        }
    }

    /// <summary>
    /// Get import statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<object>> GetImportStatistics()
    {
        try
        {
            var batches = await _importBatchRepository.GetAllAsync();
            
            var stats = new
            {
                TotalImports = batches.Count(),
                ActiveImports = batches.Count(b => !b.DeletedAt.HasValue),
                DeletedImports = batches.Count(b => b.DeletedAt.HasValue),
                
                ByType = batches.GroupBy(b => b.BatchType).Select(g => new
                {
                    Type = g.Key,
                    Count = g.Count(),
                    Active = g.Count(b => !b.DeletedAt.HasValue)
                }).ToList(),
                
                TotalRecordsProcessed = batches.Where(b => !b.DeletedAt.HasValue).Sum(b => b.TotalRecords),
                TotalSuccessRecords = batches.Where(b => !b.DeletedAt.HasValue).Sum(b => b.SuccessCount),
                TotalErrorRecords = batches.Where(b => !b.DeletedAt.HasValue).Sum(b => b.ErrorCount),
                
                LastImport = batches.Where(b => !b.DeletedAt.HasValue).OrderByDescending(b => b.ImportDate).FirstOrDefault()?.ImportDate,
                
                RecentImports = batches.Where(b => !b.DeletedAt.HasValue)
                    .OrderByDescending(b => b.ImportDate)
                    .Take(5)
                    .Select(b => new
                    {
                        b.BatchCode,
                        b.BatchType,
                        b.FileName,
                        b.ImportDate,
                        b.TotalRecords
                    }).ToList()
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting import statistics");
            return StatusCode(500, "Error interno del servidor");
        }
    }
}