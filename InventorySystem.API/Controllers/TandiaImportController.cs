using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InventorySystem.Application.Interfaces;
using InventorySystem.Application.DTOs;
using InventorySystem.API.Utilities;
using InventorySystem.Core.Interfaces;
using InventorySystem.Application.Services;

namespace InventorySystem.API.Controllers;

[Authorize(Policy = "AdminOnly")]
[ApiController]
[Route("api/[controller]")]
public class TandiaImportController : ControllerBase
{
    private readonly ITandiaImportService _tandiaImportService;
    private readonly IImportBatchRepository _importBatchRepository;
    private readonly IProductRepository _productRepository;
    private readonly IStockInitialService _stockInitialService;
    private readonly IImportLockService _importLockService;

    public TandiaImportController(ITandiaImportService tandiaImportService, IImportBatchRepository importBatchRepository, IProductRepository productRepository, IStockInitialService stockInitialService, IImportLockService importLockService)
    {
        _tandiaImportService = tandiaImportService;
        _importBatchRepository = importBatchRepository;
        _productRepository = productRepository;
        _stockInitialService = stockInitialService;
        _importLockService = importLockService;
    }

    /// <summary>
    /// Upload and import products from Tandia Excel file - SÍNCRONO (DEPRECADO: Use BackgroundJobs/products/queue)
    /// </summary>
    [HttpPost("products")]
    public async Task<ActionResult<BulkUploadResultDto>> ImportProducts(IFormFile file)
    {
        // BLOQUEO MUTUO: Verificar si se permite la importación de productos
        var isAllowed = await _importLockService.IsImportAllowedAsync("PRODUCTS_IMPORT");
        if (!isAllowed)
        {
            var blockingMessage = await _importLockService.GetBlockingJobMessageAsync("PRODUCTS_IMPORT");
            return Conflict(new 
            { 
                error = "No se puede iniciar la carga de productos en este momento",
                reason = blockingMessage,
                suggestion = "Use el endpoint BackgroundJobs/products/queue para cargas en segundo plano, o espere a que termine el proceso actual",
                warningDeprecation = "⚠️ Este endpoint síncrono está deprecado. Use BackgroundJobs/products/queue para mejor rendimiento."
            });
        }

        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        if (!FileValidationHelper.IsValidExcelFile(file))
            return BadRequest("Invalid file format. Please upload an Excel file (.xlsx or .xls)");

        try
        {
            using var stream = file.OpenReadStream();
            var result = await _tandiaImportService.ImportProductsFromExcelAsync(stream, file.FileName);
            
            if (result.ErrorCount > 0)
                return Ok(result); // Return with errors but 200 status
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Upload and import sales from Tandia Excel file - SÍNCRONO (DEPRECADO: Use BackgroundJobs/sales/queue)
    /// </summary>
    [HttpPost("sales")]
    public async Task<ActionResult<BulkUploadResultDto>> ImportSales(IFormFile file, [FromForm] string storeCode)
    {
        // BLOQUEO MUTUO: Verificar si se permite la importación de ventas
        var isAllowed = await _importLockService.IsImportAllowedAsync("SALES_IMPORT", storeCode);
        if (!isAllowed)
        {
            var blockingMessage = await _importLockService.GetBlockingJobMessageAsync("SALES_IMPORT", storeCode);
            return Conflict(new 
            { 
                error = "No se puede iniciar la carga de ventas en este momento",
                reason = blockingMessage,
                suggestion = "Use el endpoint BackgroundJobs/sales/queue para cargas en segundo plano, o espere a que termine el proceso actual",
                warningDeprecation = "⚠️ Este endpoint síncrono está deprecado. Use BackgroundJobs/sales/queue para mejor rendimiento."
            });
        }

        // Verificar si existen productos en el sistema antes de permitir importar ventas
        var allProducts = await _productRepository.GetAllAsync();
        var activeProducts = allProducts.Where(p => p.Active && !p.IsDeleted).ToList();
        
        if (!activeProducts.Any())
        {
            return BadRequest("No se pueden importar ventas porque no hay productos registrados en el sistema. Debe importar productos primero.");
        }

        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        if (string.IsNullOrEmpty(storeCode))
            return BadRequest("El código de sucursal es requerido");

        if (!FileValidationHelper.IsValidExcelFile(file))
            return BadRequest("Invalid file format. Please upload an Excel file (.xlsx or .xls)");

        try
        {
            using var stream = file.OpenReadStream();
            var result = await _tandiaImportService.ImportSalesFromExcelAsync(stream, file.FileName, storeCode);
            
            if (result.ErrorCount > 0)
                return Ok(result); // Return with errors but 200 status
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Upload and import both products and sales files at once - SÍNCRONO (DEPRECADO)
    /// </summary>
    [HttpPost("full-import")]
    public async Task<ActionResult<TandiaUploadSummaryDto>> ImportFullDataset(
        IFormFile productsFile,
        IFormFile salesFile)
    {
        // BLOQUEO MUTUO: Verificar si se permite cualquier importación
        var isAllowed = await _importLockService.IsImportAllowedAsync("PRODUCTS_IMPORT");
        if (!isAllowed)
        {
            var blockingMessage = await _importLockService.GetBlockingJobMessageAsync("PRODUCTS_IMPORT");
            return Conflict(new 
            { 
                error = "No se puede iniciar la carga completa en este momento",
                reason = blockingMessage,
                suggestion = "Use los endpoints individuales en BackgroundJobs/ para cargas en segundo plano, o espere a que termine el proceso actual",
                warningDeprecation = "⚠️ Este endpoint está deprecado. Use los endpoints individuales en BackgroundJobs/ para mejor control y rendimiento."
            });
        }

        if (productsFile == null || productsFile.Length == 0)
            return BadRequest("Products file is required");

        if (salesFile == null || salesFile.Length == 0)
            return BadRequest("Sales file is required");

        if (!FileValidationHelper.IsValidExcelFile(productsFile) || !FileValidationHelper.IsValidExcelFile(salesFile))
            return BadRequest("Invalid file format. Please upload Excel files (.xlsx or .xls)");

        try
        {
            using var productsStream = productsFile.OpenReadStream();
            using var salesStream = salesFile.OpenReadStream();
            
            var result = await _tandiaImportService.ImportFullDatasetAsync(productsStream, salesStream);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Validate products Excel file without importing
    /// </summary>
    [HttpPost("validate-products")]
    public async Task<ActionResult<object>> ValidateProducts(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        if (!FileValidationHelper.IsValidExcelFile(file))
            return BadRequest("Invalid file format. Please upload an Excel file (.xlsx or .xls)");

        try
        {
            using var stream = file.OpenReadStream();
            var products = await _tandiaImportService.ValidateProductsExcelAsync(stream);
            
            var summary = new
            {
                TotalRecords = products.Count,
                UniqueProducts = products.DistinctBy(p => p.Code).Count(),
                Categories = products.GroupBy(p => p.Categories).Select(g => new { 
                    Category = g.Key, 
                    Count = g.Count() 
                }).OrderByDescending(x => x.Count).ToList(),
                Brands = products.Where(p => !string.IsNullOrEmpty(p.Brand))
                    .GroupBy(p => p.Brand).Select(g => new { 
                    Brand = g.Key, 
                    Count = g.Count() 
                }).OrderByDescending(x => x.Count).ToList(),
                ActiveProducts = products.Count(p => p.Status.ToUpper() == "ACTIVO"),
                InactiveProducts = products.Count(p => p.Status.ToUpper() != "ACTIVO"),
                ProductsWithStock = products.Count(p => p.Stock > 0),
                ProductsWithoutStock = products.Count(p => p.Stock == 0),
                Sample = products.Take(5).ToList()
            };
            
            return Ok(summary);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Validate sales Excel file without importing
    /// </summary>
    [HttpPost("validate-sales")]
    public async Task<ActionResult<object>> ValidateSales(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        if (!FileValidationHelper.IsValidExcelFile(file))
            return BadRequest("Invalid file format. Please upload an Excel file (.xlsx or .xls)");

        try
        {
            using var stream = file.OpenReadStream();
            var sales = await _tandiaImportService.ValidateSalesExcelAsync(stream);
            
            var summary = new
            {
                TotalRecords = sales.Count,
                UniqueSales = sales.DistinctBy(s => s.DocumentNumber).Count(),
                DateRange = new {
                    From = sales.Min(s => s.Date),
                    To = sales.Max(s => s.Date)
                },
                TotalAmount = sales.Sum(s => s.Total),
                Customers = sales.Where(s => s.CustomerName != "Cliente Genérico" && s.CustomerName != "Generic Customer")
                    .GroupBy(s => s.CustomerName).Select(g => new { 
                    Customer = g.Key, 
                    Transactions = g.Count() 
                }).OrderByDescending(x => x.Transactions).Take(10).ToList(),
                Products = sales.GroupBy(s => s.ProductCode).Select(g => new { 
                    SKU = g.Key, 
                    ProductName = g.First().ProductName,
                    TotalQuantity = g.Sum(x => x.Quantity),
                    TotalSales = g.Sum(x => x.Total)
                }).OrderByDescending(x => x.TotalSales).Take(10).ToList(),
                Sample = sales.Take(5).ToList()
            };
            
            return Ok(summary);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Clear all products and related data
    /// </summary>
    [HttpDelete("products/clear")]
    public async Task<ActionResult> ClearAllProducts()
    {
        try
        {
            var result = await _tandiaImportService.ClearAllProductsAsync();
            return Ok(new
            {
                message = "Todos los productos han sido eliminados exitosamente",
                deletedProducts = result.DeletedProducts,
                deletedCategories = result.DeletedCategories,
                deletedProductStocks = result.DeletedProductStocks,
                deletedInventoryMovements = result.DeletedInventoryMovements
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Clear all sales and related data
    /// </summary>
    [HttpDelete("sales/clear")]
    public async Task<ActionResult> ClearAllSales()
    {
        try
        {
            var result = await _tandiaImportService.ClearAllSalesAsync();
            return Ok(new
            {
                message = "Todas las ventas han sido eliminadas exitosamente",
                deletedSales = result.DeletedSales,
                deletedSaleDetails = result.DeletedSaleDetails,
                deletedInventoryMovements = result.DeletedInventoryMovements
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Check if there are products in the system
    /// </summary>
    [AllowAnonymous]
    [HttpGet("products/exists")]
    public async Task<ActionResult<object>> CheckProductsExist()
    {
        try
        {
            var allProducts = await _productRepository.GetAllAsync();
            var activeProducts = allProducts.Where(p => p.Active && !p.IsDeleted).ToList();
            
            return Ok(new
            {
                hasProducts = activeProducts.Any(),
                totalActiveProducts = activeProducts.Count,
                message = activeProducts.Any() ? 
                    "Hay productos disponibles en el sistema" : 
                    "No hay productos registrados. Debe importar productos primero."
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get import history and statistics
    /// </summary>
    [HttpGet("history")]
    public async Task<ActionResult<object>> GetImportHistory()
    {
        // This would typically come from a database table tracking imports
        // For now, return a placeholder response
        var history = new
        {
            LastImport = DateTime.Now.AddDays(-1),
            TotalImports = 5,
            TotalProductsImported = 1024,
            TotalSalesImported = 418,
            LastProductsFile = "reporte_productos_tandia.xlsx",
            LastSalesFile = "detalle_de_ventas_2025_04_26_08_47_18.xlsx"
        };

        return Ok(history);
    }

    /// <summary>
    /// Get import batches with pagination and filters
    /// </summary>
    [HttpGet("import-batches")]
    public async Task<ActionResult> GetImportBatches(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? type = null,
        [FromQuery] string? status = null,
        [FromQuery] string? storeCode = null)
    {
        try
        {
            var batches = await _importBatchRepository.GetAllAsync();
            var query = batches.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(b => b.BatchType.Equals(type, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(status))
            {
                if (status.Equals("active", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(b => !b.IsDeleted);
                }
                else if (status.Equals("deleted", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(b => b.IsDeleted);
                }
            }

            if (!string.IsNullOrEmpty(storeCode))
            {
                query = query.Where(b => b.StoreCode == storeCode);
            }

            // Order by date (newest first)
            query = query.OrderByDescending(b => b.ImportDate);

            // Calculate pagination
            var totalItems = query.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var skip = (page - 1) * pageSize;

            var pagedBatches = query.Skip(skip).Take(pageSize).Select(b => new
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
                DeleteReason = b.DeleteReason,
                IsDeleted = b.IsDeleted
            }).ToList();

            return Ok(new
            {
                data = pagedBatches,
                pagination = new
                {
                    currentPage = page,
                    pageSize = pageSize,
                    totalItems = totalItems,
                    totalPages = totalPages,
                    hasNextPage = page < totalPages,
                    hasPreviousPage = page > 1
                },
                filters = new
                {
                    availableTypes = new[] { "PRODUCTS", "SALES", "STOCK_INITIAL" },
                    availableStatuses = new[] { "active", "deleted" },
                    availableStores = new[] { "TANT", "MAIN" }
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message, details = ex.ToString() });
        }
    }

    /// <summary>
    /// TEMP: Delete import batch (testing endpoint)
    /// </summary>
    [HttpDelete("import-batches/{batchCode}")]
    public async Task<ActionResult> DeleteImportBatch(string batchCode)
    {
        try
        {
            var batch = await _importBatchRepository.GetBatchByCodeAsync(batchCode);
            if (batch == null)
                return NotFound("Importación no encontrada");

            if (batch.DeletedAt.HasValue)
                return BadRequest("Esta importación ya fue eliminada");

            // Delete related data based on batch type
            int deletedRecords = 0;
            switch (batch.BatchType.ToUpper())
            {
                case "PRODUCTS":
                    // Delete products related to this batch
                    deletedRecords = await _tandiaImportService.DeleteProductsByBatchIdAsync(batch.Id);
                    break;
                case "SALES":
                    // Delete sales related to this batch
                    deletedRecords = await _tandiaImportService.DeleteSalesByBatchIdAsync(batch.Id);
                    break;
                case "STOCK_INITIAL":
                    // Delete stock records related to this batch
                    deletedRecords = await _stockInitialService.DeleteProductStocksByBatchIdAsync(batch.Id);
                    break;
            }

            // Mark the batch as deleted
            batch.IsDeleted = true;
            batch.DeletedAt = DateTime.UtcNow;
            batch.DeletedBy = User.Identity?.Name ?? "marc";
            batch.DeleteReason = "Eliminado desde interfaz de administración";

            await _importBatchRepository.UpdateAsync(batch);

            var result = new
            {
                message = $"Importación {batch.FileName} eliminada exitosamente",
                batchCode = batch.BatchCode,
                fileName = batch.FileName,
                deletedAt = batch.DeletedAt,
                deletedRecords = deletedRecords,
                batchType = batch.BatchType
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message, details = ex.ToString() });
        }
    }

}