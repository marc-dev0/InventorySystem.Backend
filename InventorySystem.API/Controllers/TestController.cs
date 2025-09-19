using Microsoft.AspNetCore.Mvc;
using InventorySystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using InventorySystem.Application.Services;
using InventorySystem.Core.Entities;
using InventorySystem.Application.DTOs;
using System.Text.Json;

namespace InventorySystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly InventoryDbContext _context;
    private readonly ExcelProcessorService _excelProcessorService;

    public TestController(InventoryDbContext context, ExcelProcessorService excelProcessorService)
    {
        _context = context;
        _excelProcessorService = excelProcessorService;
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
    public async Task<IActionResult> Health()
    {
        try
        {
            // Test database connection
            var canConnect = await _context.Database.CanConnectAsync();

            var result = new
            {
                status = canConnect ? "healthy" : "unhealthy",
                timestamp = DateTime.Now,
                checks = new
                {
                    api = "healthy",
                    database = canConnect ? "healthy" : "unhealthy"
                },
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            };

            return canConnect ? Ok(result) : StatusCode(503, result);
        }
        catch (Exception ex)
        {
            var result = new
            {
                status = "unhealthy",
                timestamp = DateTime.Now,
                checks = new
                {
                    api = "healthy",
                    database = "unhealthy"
                },
                error = ex.Message
            };

            return StatusCode(503, result);
        }
    }

    [HttpGet("sales-employees")]
    public async Task<IActionResult> TestSalesEmployees()
    {
        try
        {
            // Obtener todas las ventas recientes con información de empleados
            var salesWithEmployees = await _context.Sales
                .Include(s => s.Employee)
                .Include(s => s.Store)
                .OrderByDescending(s => s.CreatedAt)
                .Take(10)
                .Select(s => new
                {
                    SaleId = s.Id,
                    SaleNumber = s.SaleNumber,
                    SaleDate = s.SaleDate,
                    EmployeeId = s.EmployeeId,
                    EmployeeName = s.Employee != null ? s.Employee.Name : null,
                    EmployeeCode = s.Employee != null ? s.Employee.Code : null,
                    StoreName = s.Store.Name,
                    ImportSource = s.ImportSource,
                    ImportedAt = s.ImportedAt
                })
                .ToListAsync();

            // Obtener todos los empleados
            var allEmployees = await _context.Employees
                .Where(e => !e.IsDeleted)
                .Select(e => new
                {
                    Id = e.Id,
                    Code = e.Code,
                    Name = e.Name,
                    StoreId = e.StoreId,
                    Active = e.Active,
                    CreatedAt = e.CreatedAt
                })
                .ToListAsync();

            // Estadísticas
            var totalSales = await _context.Sales.CountAsync();
            var salesWithEmployees_Count = await _context.Sales.Where(s => s.EmployeeId != null).CountAsync();
            var salesWithoutEmployees_Count = await _context.Sales.Where(s => s.EmployeeId == null).CountAsync();

            var result = new
            {
                message = "Diagnóstico de ventas y empleados",
                statistics = new
                {
                    totalSales = totalSales,
                    salesWithEmployees = salesWithEmployees_Count,
                    salesWithoutEmployees = salesWithoutEmployees_Count,
                    totalEmployees = allEmployees.Count
                },
                recentSales = salesWithEmployees,
                employees = allEmployees
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    [HttpPost("debug-excel")]
    public async Task<IActionResult> DebugExcelFile(IFormFile file, [FromForm] string storeCode)
    {
        try
        {
            using var stream = file.OpenReadStream();
            var result = await _excelProcessorService.ProcessSalesAsync(stream, file.FileName, storeCode);
            
            return Ok(new { 
                totalRecords = result.TotalRecords,
                firstFiveRecords = result.Data.Take(5).ToList(),
                dataCount = result.Data?.Count ?? 0,
                warnings = result.Warnings,
                errors = result.Errors
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    [HttpPost("debug-excel-raw")]
    public async Task<IActionResult> DebugExcelRawData(IFormFile file)
    {
        try
        {
            using var stream = file.OpenReadStream();
            using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
            var worksheet = workbook.Worksheets.First();
            
            var rawData = new List<object>();
            var rows = worksheet.RowsUsed().Take(10); // First 10 rows
            
            foreach (var row in rows)
            {
                var rowData = new Dictionary<string, object>();
                for (int col = 1; col <= 15; col++) // Check first 15 columns
                {
                    try
                    {
                        var cellValue = row.Cell(col).GetString().Trim();
                        rowData[$"Column_{col}"] = cellValue;
                    }
                    catch
                    {
                        rowData[$"Column_{col}"] = "";
                    }
                }
                rowData["RowNumber"] = row.RowNumber();
                rawData.Add(rowData);
            }
            
            return Ok(new { 
                message = "Raw Excel data for first 10 rows and 15 columns",
                data = rawData
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    [HttpPost("create-test-sales")]
    public async Task<IActionResult> CreateTestSales()
    {
        try
        {
            // Create a test store first
            var store = new Store { Code = "TANT", Name = "Tienda Tantamayo", Description = "Test Store", Active = true };
            _context.Stores.Add(store);
            
            // Create test customer
            var customer = new Customer { Name = "Cliente Test", Document = "12345678", Active = true };
            _context.Customers.Add(customer);
            
            // Use existing seeded category
            var category = await _context.Categories.FirstOrDefaultAsync();
            if (category == null)
            {
                // Create test category if none exist
                category = new Category { Name = "Categoría Test", Description = "Test category", Active = true };
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
            }
            
            // Create test product
            var product = new Product
            {
                Code = "PROD001",
                Name = "Producto Test",
                SalePrice = 10.50m,
                Active = true,
                CategoryId = category.Id
            };
            _context.Products.Add(product);
            
            await _context.SaveChangesAsync();
            
            // Create test sale
            var sale = new Sale
            {
                SaleNumber = "SALE001",
                SaleDate = DateTime.Now,
                CustomerId = customer.Id,
                StoreId = store.Id,
                SubTotal = 10.50m,
                Taxes = 0,
                Total = 10.50m,
                ImportSource = "TEST"
            };
            
            _context.Sales.Add(sale);
            await _context.SaveChangesAsync();
            
            // Create sale detail
            var saleDetail = new SaleDetail
            {
                SaleId = sale.Id,
                ProductId = product.Id,
                Quantity = 1,
                UnitPrice = 10.50m,
                Subtotal = 10.50m
            };
            
            _context.SaleDetails.Add(saleDetail);
            await _context.SaveChangesAsync();
            
            return Ok(new { 
                message = "Test sales created successfully",
                saleId = sale.Id,
                storeId = store.Id,
                customerId = customer.Id,
                productId = product.Id
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    [HttpPost("setup-sales-config")]
    public async Task<IActionResult> SetupSalesConfiguration()
    {
        try
        {
            // Configuración de columnas esperadas
            var expectedColumns = new List<string>
            {
                "Razón Social", "Empleado Venta", "Almacén", "Cliente Nombre", "Cliente Doc.",
                "#-DOC", "# Doc. Relacionado", "Fecha", "Hora", "Tip. Doc.", "Unidad",
                "Cantidad", "Precio de Venta", "IGV", "Total", "Descuento aplicado (%)",
                "Conversión", "Moneda", "Codigo SKU", "Cod. alternativo", "Marca",
                "Categoría", "Características", "Nombre", "Descripción", "Proveedor",
                "Precio de costo", "Empleado registro"
            };

            var columnsConfig = new SystemConfiguration
            {
                ConfigKey = "IMPORT_COLUMNS_SALES",
                ConfigValue = JsonSerializer.Serialize(expectedColumns),
                ConfigType = "Json",
                Category = "Import",
                Description = "Columnas esperadas para importación de ventas",
                Active = true
            };

            // Configuración de mapeo de columnas
            var columnMapping = new SalesColumnMapping();
            var mappingConfig = new SystemConfiguration
            {
                ConfigKey = "IMPORT_MAPPING_SALES",
                ConfigValue = JsonSerializer.Serialize(columnMapping),
                ConfigType = "Json",
                Category = "Import",
                Description = "Mapeo de posiciones de columnas para importación de ventas",
                Active = true
            };

            // Verificar si ya existen configuraciones
            var existingColumns = await _context.SystemConfigurations
                .FirstOrDefaultAsync(c => c.ConfigKey == "IMPORT_COLUMNS_SALES");
            var existingMapping = await _context.SystemConfigurations
                .FirstOrDefaultAsync(c => c.ConfigKey == "IMPORT_MAPPING_SALES");

            if (existingColumns != null)
            {
                existingColumns.ConfigValue = columnsConfig.ConfigValue;
                existingColumns.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.SystemConfigurations.Add(columnsConfig);
            }

            if (existingMapping != null)
            {
                existingMapping.ConfigValue = mappingConfig.ConfigValue;
                existingMapping.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.SystemConfigurations.Add(mappingConfig);
            }

            await _context.SaveChangesAsync();

            return Ok(new 
            { 
                message = "Configuración de ventas establecida correctamente",
                expectedColumns = expectedColumns,
                columnMapping = columnMapping
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    [HttpPost("create-store")]
    public async Task<IActionResult> CreateTestStore()
    {
        try
        {
            var existingStore = await _context.Stores.FirstOrDefaultAsync(s => s.Code == "TANT");
            if (existingStore != null)
            {
                return Ok(new { message = "Store TANT already exists", store = existingStore });
            }

            var store = new Store
            {
                Code = "TANT",
                Name = "Tienda Tantamayo",
                Description = "Test Store for Sales Import",
                Active = true
            };

            _context.Stores.Add(store);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Store TANT created successfully", store = store });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("sync-product-stocks")]
    public async Task<IActionResult> SyncProductStocks()
    {
        try
        {
            var results = new List<object>();
            
            // Get all products with their current ProductStocks for store 1 (main store)
            var products = await _context.Products
                .Where(p => !p.IsDeleted)
                .ToListAsync();
                
            var store = await _context.Stores.FirstOrDefaultAsync(s => s.Code == "TIETAN");
            if (store == null)
            {
                return BadRequest("Store TANT not found");
            }
            
            int synced = 0;
            int created = 0;
            
            foreach (var product in products)
            {
                var productStock = await _context.ProductStocks
                    .FirstOrDefaultAsync(ps => ps.ProductId == product.Id && ps.StoreId == store.Id);
                    
                if (productStock == null)
                {
                    // Create missing ProductStock record
                    productStock = new ProductStock
                    {
                        ProductId = product.Id,
                        StoreId = store.Id,
                        CurrentStock = 0, // Start with 0 stock
                        MinimumStock = 0, // Default minimum stock
                        MaximumStock = 1000, // Default max stock
                        AverageCost = product.PurchasePrice
                    };
                    _context.ProductStocks.Add(productStock);
                    created++;
                    
                    results.Add(new {
                        Action = "CREATED",
                        ProductId = product.Id,
                        ProductName = product.Name,
                        ProductStock = 0, // Always starts with 0
                        NewCurrentStock = productStock.CurrentStock
                    });
                }
                // Note: Stock sync logic removed since Product.Stock field no longer exists
                // Stock is now managed exclusively through ProductStocks table
            }
            
            await _context.SaveChangesAsync();
            
            return Ok(new 
            { 
                message = "Stock synchronization completed",
                summary = new
                {
                    totalProducts = products.Count,
                    created = created,
                    synced = synced,
                    unchanged = products.Count - created - synced
                },
                details = results.Take(20).ToList(), // Show first 20 changes
                totalChanges = results.Count
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    [HttpGet("import-batches")]
    public async Task<IActionResult> TestImportBatches()
    {
        try
        {
            // Obtener todos los ImportBatches
            var importBatches = await _context.ImportBatches
                .OrderByDescending(ib => ib.CreatedAt)
                .Select(ib => new
                {
                    Id = ib.Id,
                    BatchCode = ib.BatchCode,
                    BatchType = ib.BatchType,
                    FileName = ib.FileName,
                    StoreCode = ib.StoreCode,
                    ImportedBy = ib.ImportedBy,
                    TotalRecords = ib.TotalRecords,
                    SuccessCount = ib.SuccessCount,
                    ErrorCount = ib.ErrorCount,
                    SkippedCount = ib.SkippedCount,
                    ProcessingTimeSeconds = ib.ProcessingTimeSeconds,
                    IsInProgress = ib.IsInProgress,
                    CreatedAt = ib.CreatedAt,
                    CompletedAt = ib.CompletedAt,
                    HasWarnings = !string.IsNullOrEmpty(ib.Warnings),
                    HasErrors = !string.IsNullOrEmpty(ib.Errors)
                })
                .Take(10)
                .ToListAsync();

            // Obtener BackgroundJobs relacionados
            var backgroundJobs = await _context.BackgroundJobs
                .OrderByDescending(bj => bj.StartedAt)
                .Select(bj => new
                {
                    Id = bj.Id,
                    JobId = bj.JobId,
                    JobType = bj.JobType,
                    FileName = bj.FileName,
                    Status = bj.Status,
                    ImportBatchId = bj.ImportBatchId,
                    TotalRecords = bj.TotalRecords,
                    ProcessedRecords = bj.ProcessedRecords,
                    SuccessRecords = bj.SuccessRecords,
                    ErrorRecords = bj.ErrorRecords,
                    WarningRecords = bj.WarningRecords,
                    StartedAt = bj.StartedAt,
                    CompletedAt = bj.CompletedAt,
                    DetailedWarningsCount = bj.DetailedWarnings != null ? bj.DetailedWarnings.Count : 0,
                    DetailedErrorsCount = bj.DetailedErrors != null ? bj.DetailedErrors.Count : 0
                })
                .Take(10)
                .ToListAsync();

            var result = new
            {
                message = "Diagnóstico de ImportBatches y BackgroundJobs",
                statistics = new
                {
                    totalImportBatches = importBatches.Count,
                    totalBackgroundJobs = backgroundJobs.Count,
                    inProgressBatches = importBatches.Count(ib => ib.IsInProgress),
                    completedBatches = importBatches.Count(ib => !ib.IsInProgress && ib.CompletedAt != null)
                },
                importBatches = importBatches,
                backgroundJobs = backgroundJobs
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }
}