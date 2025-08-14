using Microsoft.AspNetCore.Mvc;
using InventorySystem.Application.Interfaces;

namespace InventorySystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockInitialController : ControllerBase
{
    private readonly IStockInitialService _stockInitialService;
    private readonly ILogger<StockInitialController> _logger;

    public StockInitialController(IStockInitialService stockInitialService, ILogger<StockInitialController> logger)
    {
        _stockInitialService = stockInitialService;
        _logger = logger;
    }

    /// <summary>
    /// Cargar stock inicial desde archivo Excel
    /// </summary>
    [HttpPost("upload-excel")]
    public async Task<IActionResult> UploadStockExcel(IFormFile file, [FromForm] string storeCode)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No se ha seleccionado ningún archivo.");
            }

            if (!IsValidExcelFile(file))
            {
                return BadRequest("El archivo debe ser un Excel (.xlsx o .xls).");
            }

            if (string.IsNullOrEmpty(storeCode))
            {
                return BadRequest("Debe especificar el código del almacén (TANT para tienda, MAIN para almacén).");
            }

            // Validar códigos de store permitidos
            var allowedStoreCodes = new[] { "TANT", "MAIN" };
            if (!allowedStoreCodes.Contains(storeCode.ToUpper()))
            {
                return BadRequest($"Código de almacén inválido. Códigos permitidos: {string.Join(", ", allowedStoreCodes)}");
            }

            _logger.LogInformation("Iniciando carga de stock desde archivo: {FileName}, Almacén: {StoreCode}", 
                file.FileName, storeCode);

            using var stream = file.OpenReadStream();
            var result = await _stockInitialService.LoadStockFromExcelAsync(stream, file.FileName, storeCode.ToUpper());

            if (result.Errors.Any())
            {
                _logger.LogWarning("Carga completada con errores. Procesados: {Processed}, Errores: {Errors}", 
                    result.ProcessedProducts, result.Errors.Count);
                
                return BadRequest(new 
                { 
                    message = "Carga completada con errores",
                    processedProducts = result.ProcessedProducts,
                    skippedProducts = result.SkippedProducts,
                    totalStock = result.TotalStock,
                    storeName = result.StoreName,
                    errors = result.Errors,
                    warnings = result.Warnings
                });
            }

            return Ok(new 
            { 
                message = "Stock cargado exitosamente",
                processedProducts = result.ProcessedProducts,
                skippedProducts = result.SkippedProducts,
                totalStock = result.TotalStock,
                storeName = result.StoreName,
                warnings = result.Warnings
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar stock desde Excel");
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }

    /// <summary>
    /// Limpiar todo el stock existente (ProductStocks e InventoryMovements)
    /// </summary>
    [HttpPost("clear-stock")]
    public async Task<IActionResult> ClearAllStock()
    {
        try
        {
            _logger.LogInformation("Iniciando limpieza de stock...");
            
            var result = await _stockInitialService.ClearAllStockAsync();
            
            _logger.LogInformation("Stock limpiado. ProductStocks: {ProductStocks}, Movements: {Movements}, Products reset: {Products}", 
                result.ClearedProductStocks, result.ClearedMovements, result.ResetProducts);
            
            return Ok(new 
            { 
                message = "Stock limpiado exitosamente",
                clearedProductStocks = result.ClearedProductStocks,
                clearedMovements = result.ClearedMovements,
                resetProducts = result.ResetProducts
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al limpiar stock");
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }

    /// <summary>
    /// Limpiar stock de un almacén específico
    /// </summary>
    [HttpPost("clear-stock/{storeCode}")]
    public async Task<IActionResult> ClearStockByStore(string storeCode)
    {
        try
        {
            // Validar código de store
            var allowedStoreCodes = new[] { "TANT", "MAIN" };
            if (!allowedStoreCodes.Contains(storeCode.ToUpper()))
            {
                return BadRequest($"Código de almacén inválido. Códigos permitidos: {string.Join(", ", allowedStoreCodes)}");
            }

            _logger.LogInformation("Iniciando limpieza de stock para almacén: {StoreCode}", storeCode);
            
            var result = await _stockInitialService.ClearStockByStoreAsync(storeCode.ToUpper());
            
            _logger.LogInformation("Stock de {StoreName} limpiado. ProductStocks: {ProductStocks}, Movements: {Movements}", 
                result.StoreName, result.ClearedProductStocks, result.ClearedMovements);
            
            return Ok(new 
            { 
                message = $"Stock del almacén {result.StoreName} limpiado exitosamente",
                storeCode = result.StoreCode,
                storeName = result.StoreName,
                clearedProductStocks = result.ClearedProductStocks,
                clearedMovements = result.ClearedMovements
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al limpiar stock del almacén {StoreCode}", storeCode);
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtener resumen del stock actual
    /// </summary>
    [HttpGet("stock-summary")]
    public async Task<IActionResult> GetStockSummary()
    {
        try
        {
            var summary = await _stockInitialService.GetStockSummaryAsync();
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener resumen de stock");
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtener información de los almacenes disponibles
    /// </summary>
    [HttpGet("stores")]
    public IActionResult GetAvailableStores()
    {
        var stores = new[]
        {
            new { Code = "TANT", Name = "Tienda Tantamayo", Description = "Tienda principal" },
            new { Code = "MAIN", Name = "Almacén Principal", Description = "Almacén central" }
        };

        return Ok(stores);
    }

    private static bool IsValidExcelFile(IFormFile file)
    {
        var allowedExtensions = new[] { ".xlsx", ".xls" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        return allowedExtensions.Contains(fileExtension) && 
               (file.ContentType.Contains("spreadsheet") || 
                file.ContentType.Contains("excel"));
    }
}