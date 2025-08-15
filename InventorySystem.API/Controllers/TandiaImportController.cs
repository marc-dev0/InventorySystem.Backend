using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InventorySystem.Application.Interfaces;
using InventorySystem.Application.DTOs;
using InventorySystem.API.Utilities;

namespace InventorySystem.API.Controllers;

[Authorize(Policy = "AdminOnly")]
[ApiController]
[Route("api/[controller]")]
public class TandiaImportController : ControllerBase
{
    private readonly ITandiaImportService _tandiaImportService;

    public TandiaImportController(ITandiaImportService tandiaImportService)
    {
        _tandiaImportService = tandiaImportService;
    }

    /// <summary>
    /// Upload and import products from Tandia Excel file
    /// </summary>
    [HttpPost("products")]
    public async Task<ActionResult<BulkUploadResultDto>> ImportProducts(IFormFile file)
    {
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
    /// Upload and import sales from Tandia Excel file
    /// </summary>
    [HttpPost("sales")]
    public async Task<ActionResult<BulkUploadResultDto>> ImportSales(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        if (!FileValidationHelper.IsValidExcelFile(file))
            return BadRequest("Invalid file format. Please upload an Excel file (.xlsx or .xls)");

        try
        {
            using var stream = file.OpenReadStream();
            var result = await _tandiaImportService.ImportSalesFromExcelAsync(stream, file.FileName);
            
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
    /// Upload and import both products and sales files at once
    /// </summary>
    [HttpPost("full-import")]
    public async Task<ActionResult<TandiaUploadSummaryDto>> ImportFullDataset(
        IFormFile productsFile,
        IFormFile salesFile)
    {
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

}