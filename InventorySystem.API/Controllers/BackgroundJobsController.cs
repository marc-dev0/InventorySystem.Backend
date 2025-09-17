using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InventorySystem.Application.Interfaces;
using InventorySystem.Core.Entities;
using InventorySystem.Core.Interfaces;
using InventorySystem.Application.Services;

namespace InventorySystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BackgroundJobsController : ControllerBase
{
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly IProductRepository _productRepository;
    private readonly IStockValidationService _stockValidationService;
    private readonly ITandiaImportService _tandiaImportService;
    private readonly IImportLockService _importLockService;
    private readonly IProductStockRepository _productStockRepository;
    private readonly IStoreRepository _storeRepository;

    public BackgroundJobsController(
        IBackgroundJobService backgroundJobService,
        IProductRepository productRepository,
        IStockValidationService stockValidationService,
        ITandiaImportService tandiaImportService,
        IImportLockService importLockService,
        IProductStockRepository productStockRepository,
        IStoreRepository storeRepository)
    {
        _backgroundJobService = backgroundJobService;
        _productRepository = productRepository;
        _stockValidationService = stockValidationService;
        _tandiaImportService = tandiaImportService;
        _importLockService = importLockService;
        _productStockRepository = productStockRepository;
        _storeRepository = storeRepository;
    }

    /// <summary>
    /// Queue sales import as background job
    /// </summary>
    [HttpPost("sales/queue")]
    [AllowAnonymous] // Temporal para testing
    //[Authorize(Policy = "AdminOnly")] // ALTA SEGURIDAD: Solo admins pueden cargar datos masivos
    public async Task<ActionResult<object>> QueueSalesImport(IFormFile file, [FromForm] string storeCode)
    {
        // 1. Verificar si se permite la importación de ventas
        var isAllowed = await _importLockService.IsImportAllowedAsync("SALES_IMPORT", storeCode);
        if (!isAllowed)
        {
            var blockingMessage = await _importLockService.GetBlockingJobMessageAsync("SALES_IMPORT", storeCode);
            return Conflict(new 
            { 
                error = "No se puede iniciar la carga de ventas en este momento",
                reason = blockingMessage,
                suggestion = "Espere a que termine la carga actual o consulte el estado de los jobs activos"
            });
        }

        // 2. Verificar si existen productos en el sistema antes de permitir importar ventas
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

        try
        {
            var userId = User.Identity?.Name ?? "Unknown";
            using var stream = file.OpenReadStream();
            
            var jobId = await _backgroundJobService.QueueSalesImportAsync(stream, file.FileName, storeCode, userId);
            
            return Ok(new
            {
                message = "Importación de ventas encolada exitosamente",
                jobId = jobId,
                status = "QUEUED",
                fileName = file.FileName,
                storeCode = storeCode,
                note = "La importación se está procesando en segundo plano. Use el jobId para consultar el progreso.",
                warning = "⚠️ Durante esta carga no se podrán realizar NINGUNA otra importación (productos o stock). Solo se permite un proceso a la vez."
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Queue stock import as background job
    /// </summary>
    [HttpPost("stock/queue")]
    [AllowAnonymous] // Temporal para testing
    //[Authorize(Policy = "AdminOnly")] // ALTA SEGURIDAD: Solo admins pueden cargar datos masivos
    public async Task<ActionResult<object>> QueueStockImport(IFormFile file, [FromForm] string storeCode)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        if (string.IsNullOrEmpty(storeCode))
            return BadRequest("El código de sucursal es requerido");

        // Verificar que la tienda existe
        var store = await _storeRepository.GetByCodeAsync(storeCode);
        if (store == null)
            return BadRequest($"No se encontró la tienda con código: {storeCode}");

        // VALIDACIÓN CRÍTICA: Verificar si la tienda ya tiene stock inicial cargado
        // Use the actual Store.HasInitialStock field instead of checking ProductStocks existence
        // This prevents product imports from incorrectly blocking stock initial uploads
        if (store.HasInitialStock)
        {
            return Conflict(new 
            { 
                error = "Stock inicial ya cargado",
                message = $"La tienda '{store.Name}' ({storeCode}) ya tiene stock inicial cargado.",
                reason = "Solo se permite UNA carga de stock inicial por tienda. Los movimientos de stock posteriores se deben realizar mediante ventas u otros módulos de movimiento de inventario.",
                suggestion = "Si necesita actualizar el stock, use los módulos de ajuste de inventario o movimientos de stock."
            });
        }

        // Verificar si se permite la importación de stock (procesos concurrentes)
        var isAllowed = await _importLockService.IsImportAllowedAsync("STOCK_IMPORT", storeCode);
        if (!isAllowed)
        {
            var blockingMessage = await _importLockService.GetBlockingJobMessageAsync("STOCK_IMPORT", storeCode);
            return Conflict(new 
            { 
                error = "No se puede iniciar la carga de stock en este momento",
                reason = blockingMessage,
                suggestion = "Espere a que termine la carga actual o consulte el estado de los jobs activos"
            });
        }

        try
        {
            var userId = User.Identity?.Name ?? "Unknown";
            using var stream = file.OpenReadStream();
            
            var jobId = await _backgroundJobService.QueueStockImportAsync(stream, file.FileName, storeCode, userId);
            
            return Ok(new
            {
                message = "Importación de stock inicial encolada exitosamente",
                jobId = jobId,
                status = "QUEUED",
                fileName = file.FileName,
                storeCode = storeCode,
                note = "La importación se está procesando en segundo plano. Use el jobId para consultar el progreso.",
                warning = "⚠️ Esta es una carga de STOCK INICIAL única. Una vez completada, no se podrá volver a cargar stock inicial para esta tienda.",
                importantNote = "Durante esta carga no se podrán realizar NINGUNA otra importación (productos o ventas). Solo se permite un proceso a la vez."
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Queue products import as background job
    /// </summary>
    [HttpPost("products/queue")]
    [AllowAnonymous] // Temporal para testing
    //[Authorize(Policy = "AdminOnly")] // ALTA SEGURIDAD: Solo admins pueden cargar datos masivos
    public async Task<ActionResult<object>> QueueProductsImport(IFormFile file)
    {
        // Verificar si se permite la importación de productos
        var isAllowed = await _importLockService.IsImportAllowedAsync("PRODUCTS_IMPORT");
        if (!isAllowed)
        {
            var blockingMessage = await _importLockService.GetBlockingJobMessageAsync("PRODUCTS_IMPORT");
            return Conflict(new 
            { 
                error = "No se puede iniciar la carga de productos en este momento",
                reason = blockingMessage,
                suggestion = "Espere a que terminen todas las cargas activas antes de importar productos"
            });
        }

        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        try
        {
            var userId = User.Identity?.Name ?? "Unknown";
            using var stream = file.OpenReadStream();
            
            var jobId = await _backgroundJobService.QueueProductsImportAsync(stream, file.FileName, userId);
            
            return Ok(new
            {
                message = "Importación de productos encolada exitosamente",
                jobId = jobId,
                status = "QUEUED",
                fileName = file.FileName,
                note = "La importación se está procesando en segundo plano. Use el jobId para consultar el progreso.",
                warning = "⚠️ Durante esta carga no se podrán realizar NINGUNA otra importación (ventas o stock). Solo se permite un proceso a la vez."
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get job status and progress
    /// </summary>
    [HttpGet("{jobId}/status")]
    [AllowAnonymous] // Temporal para testing
    //[Authorize(Policy = "UserOrAdmin")] // CONSULTA: Usuarios pueden ver el estado de sus jobs
    public async Task<ActionResult<object>> GetJobStatus(string jobId)
    {
        try
        {
            var job = await _backgroundJobService.GetJobStatusAsync(jobId);
            
            if (job == null)
                return NotFound("Job not found");

            return Ok(new
            {
                jobId = job.JobId,
                jobType = job.JobType,
                status = job.Status,
                fileName = job.FileName,
                storeCode = job.StoreCode,
                totalRecords = job.TotalRecords,
                processedRecords = job.ProcessedRecords,
                successRecords = job.SuccessRecords,
                errorRecords = job.ErrorRecords,
                warningRecords = job.WarningRecords,
                progressPercentage = job.ProgressPercentage,
                startedAt = job.StartedAt,
                completedAt = job.CompletedAt,
                errorMessage = job.ErrorMessage,
                warningMessage = job.WarningMessage,
                detailedErrors = job.DetailedErrors,
                detailedWarnings = job.DetailedWarnings,
                startedBy = job.StartedBy,
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get user's recent jobs
    /// </summary>
    [HttpGet("my-jobs")]
    [Authorize(Policy = "UserOrAdmin")] // CONSULTA: Usuarios pueden ver sus propios jobs
    public async Task<ActionResult<List<object>>> GetMyJobs()
    {
        try
        {
            var userId = User.Identity?.Name ?? "Unknown";
            var jobs = await _backgroundJobService.GetUserJobsAsync(userId);
            
            var result = jobs.Select(job => new
            {
                jobId = job.JobId,
                jobType = job.JobType,
                status = job.Status,
                fileName = job.FileName,
                storeCode = job.StoreCode,
                totalRecords = job.TotalRecords,
                processedRecords = job.ProcessedRecords,
                successRecords = job.SuccessRecords,
                errorRecords = job.ErrorRecords,
                warningRecords = job.WarningRecords,
                progressPercentage = job.ProgressPercentage,
                startedAt = job.StartedAt,
                completedAt = job.CompletedAt,
                errorMessage = job.ErrorMessage
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get recent jobs (admin only)
    /// </summary>
    [HttpGet("recent")]
    [Authorize(Policy = "AdminOnly")] // ADMIN: Solo admins ven todos los jobs del sistema
    public async Task<ActionResult<List<object>>> GetRecentJobs([FromQuery] int count = 20)
    {
        try
        {
            var jobs = await _backgroundJobService.GetRecentJobsAsync(count);
            
            var result = jobs.Select(job => new
            {
                jobId = job.JobId,
                jobType = job.JobType,
                status = job.Status,
                fileName = job.FileName,
                storeCode = job.StoreCode,
                totalRecords = job.TotalRecords,
                processedRecords = job.ProcessedRecords,
                successRecords = job.SuccessRecords,
                errorRecords = job.ErrorRecords,
                warningRecords = job.WarningRecords,
                progressPercentage = job.ProgressPercentage,
                startedAt = job.StartedAt,
                completedAt = job.CompletedAt,
                startedBy = job.StartedBy,
                errorMessage = job.ErrorMessage
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Validate sales file for stock impact before queuing
    /// </summary>
    [HttpPost("sales/validate-stock")]
    [Authorize(Policy = "AdminOnly")] // VALIDACIÓN: Solo admins pueden validar archivos para carga
    public async Task<ActionResult<object>> ValidateSalesStock(IFormFile file, [FromForm] string storeCode)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        if (string.IsNullOrEmpty(storeCode))
            return BadRequest("El código de sucursal es requerido");

        try
        {
            using var stream = file.OpenReadStream();
            
            // Leer los datos del archivo
            var salesData = await _tandiaImportService.ValidateSalesExcelAsync(stream);
            
            // Validar impacto en stock
            var stockValidation = await _stockValidationService.ValidateSalesStockImpactAsync(salesData, storeCode);
            
            return Ok(new
            {
                isValid = stockValidation.IsValid,
                totalProducts = stockValidation.TotalProducts,
                productsWithIssues = stockValidation.ProductsWithIssues,
                criticalIssues = stockValidation.CriticalIssues,
                warningIssues = stockValidation.WarningIssues,
                validationSummary = stockValidation.ValidationSummary,
                validationDate = stockValidation.ValidationDate,
                issues = stockValidation.Issues.Select(issue => new
                {
                    productCode = issue.ProductCode,
                    productName = issue.ProductName,
                    storeCode = issue.StoreCode,
                    currentStock = issue.CurrentStock,
                    requestedQuantity = issue.RequestedQuantity,
                    resultingStock = issue.ResultingStock,
                    issueType = issue.IssueType.ToString(),
                    description = issue.Description,
                    recommendation = issue.Recommendation,
                    saleLineNumber = issue.SaleLineNumber,
                    documentNumber = issue.DocumentNumber
                }).ToList(),
                recommendation = stockValidation.IsValid 
                    ? "✅ Archivo válido para procesamiento" 
                    : stockValidation.CriticalIssues > salesData.Count * 0.1 
                        ? "❌ Demasiados errores críticos. Revisar inventario antes de procesar." 
                        : "⚠️ Hay errores pero el archivo puede procesarse con precaución."
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Validate current stock levels for a store
    /// </summary>
    [HttpGet("stock/validate/{storeCode}")]
    [Authorize(Policy = "UserOrAdmin")] // CONSULTA: Usuarios pueden validar stock para ver estado
    public async Task<ActionResult<object>> ValidateStoreStock(string storeCode)
    {
        if (string.IsNullOrEmpty(storeCode))
            return BadRequest("El código de sucursal es requerido");

        try
        {
            var stockValidation = await _stockValidationService.ValidateCurrentStockLevelsAsync(storeCode);
            
            return Ok(new
            {
                isValid = stockValidation.IsValid,
                storeCode = storeCode,
                totalProducts = stockValidation.TotalProducts,
                productsWithIssues = stockValidation.ProductsWithIssues,
                criticalIssues = stockValidation.CriticalIssues,
                warningIssues = stockValidation.WarningIssues,
                validationSummary = stockValidation.ValidationSummary,
                validationDate = stockValidation.ValidationDate,
                issues = stockValidation.Issues.Select(issue => new
                {
                    productCode = issue.ProductCode,
                    productName = issue.ProductName,
                    currentStock = issue.CurrentStock,
                    issueType = issue.IssueType.ToString(),
                    description = issue.Description,
                    recommendation = issue.Recommendation
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get current stock for a specific product
    /// </summary>
    [HttpGet("stock/{productCode}/{storeCode}")]
    [Authorize(Policy = "UserOrAdmin")] // CONSULTA: Usuarios pueden consultar stock específico
    public async Task<ActionResult<object>> GetProductStock(string productCode, string storeCode)
    {
        try
        {
            var currentStock = await _stockValidationService.GetCurrentStockAsync(productCode, storeCode);
            
            return Ok(new
            {
                productCode = productCode,
                storeCode = storeCode,
                currentStock = currentStock,
                status = currentStock > 0 ? "AVAILABLE" : currentStock == 0 ? "OUT_OF_STOCK" : "NEGATIVE_STOCK"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Clean up stale background jobs
    /// </summary>
    [HttpPost("cleanup")]
    [Authorize(Policy = "AdminOnly")] // ADMIN: Solo admins pueden limpiar jobs
    public async Task<ActionResult<object>> CleanupStaleJobs()
    {
        try
        {
            await _importLockService.CleanupStaleJobsAsync();
            
            return Ok(new
            {
                message = "Jobs obsoletos limpiados exitosamente",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Check if imports are allowed and get active jobs status - BLOQUEO MUTUO
    /// </summary>
    [HttpGet("status/imports")]
    [AllowAnonymous] // Temporal para testing
    //[Authorize(Policy = "UserOrAdmin")] // CONSULTA: Usuarios pueden ver estado general de importaciones
    public async Task<ActionResult<object>> GetImportsStatus([FromQuery] string? storeCode = null)
    {
        try
        {
            // BLOQUEO MUTUO: Verificar si hay algún trabajo activo
            var hasAnyActiveJobs = await _importLockService.HasAnyActiveJobsAsync();
            var activeJobs = await _importLockService.GetAllActiveJobsAsync();
            
            // Con bloqueo mutuo, todos los imports están permitidos solo si NO hay trabajos activos
            var allImportsAllowed = !hasAnyActiveJobs;
            
            var blockingMessage = hasAnyActiveJobs 
                ? await _importLockService.GetBlockingJobMessageAsync("SALES_IMPORT", storeCode)
                : null;

            return Ok(new
            {
                storeCode = storeCode,
                mutualExclusion = new
                {
                    isEnabled = true,
                    hasAnyActiveJobs = hasAnyActiveJobs,
                    totalActiveJobs = activeJobs.Count,
                    activeJobsList = activeJobs.Select(job => new 
                    {
                        jobId = job.JobId,
                        jobType = job.JobType,
                        status = job.Status,
                        storeCode = job.StoreCode,
                        progressPercentage = job.ProgressPercentage,
                        startedAt = job.StartedAt
                    }).ToList()
                },
                imports = new
                {
                    sales = new
                    {
                        allowed = allImportsAllowed,
                        blockingMessage = blockingMessage
                    },
                    stock = new
                    {
                        allowed = allImportsAllowed,
                        blockingMessage = blockingMessage
                    },
                    products = new
                    {
                        allowed = allImportsAllowed,
                        blockingMessage = blockingMessage
                    }
                },
                deletions = new
                {
                    products = await _importLockService.CanDeleteAsync("PRODUCT", 0, storeCode),
                    sales = await _importLockService.CanDeleteAsync("SALE", 0, storeCode),
                    customers = await _importLockService.CanDeleteAsync("CUSTOMER", 0, storeCode)
                },
                summary = new
                {
                    systemStatus = hasAnyActiveJobs ? "BUSY" : "AVAILABLE",
                    message = hasAnyActiveJobs 
                        ? "Sistema ocupado: Solo se permite un proceso a la vez"
                        : "Sistema disponible: Se puede iniciar cualquier proceso",
                    canPerformAnyImport = allImportsAllowed,
                    canPerformDeletions = !hasAnyActiveJobs
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Check if a specific entity can be deleted
    /// </summary>
    [HttpGet("status/can-delete/{entityType}/{entityId}")]
    [Authorize(Policy = "UserOrAdmin")] // CONSULTA: Usuarios pueden verificar si pueden eliminar algo
    public async Task<ActionResult<object>> CanDeleteEntity(string entityType, int entityId, [FromQuery] string? storeCode = null)
    {
        try
        {
            var canDelete = await _importLockService.CanDeleteAsync(entityType, entityId, storeCode);
            var blockingMessage = "";

            if (!canDelete)
            {
                // Buscar qué jobs están bloqueando
                var activeSales = await _importLockService.HasActiveJobsAsync("SALES_IMPORT", storeCode);
                var activeStock = await _importLockService.HasActiveJobsAsync("STOCK_IMPORT", storeCode);
                var activeProducts = await _importLockService.HasActiveJobsAsync("PRODUCTS_IMPORT");

                var blockingJobs = new List<string>();
                if (activeSales) blockingJobs.Add("carga de ventas");
                if (activeStock) blockingJobs.Add("carga de stock");
                if (activeProducts) blockingJobs.Add("carga de productos");

                blockingMessage = blockingJobs.Any() 
                    ? $"No se puede eliminar {entityType} porque hay {string.Join(", ", blockingJobs)} en proceso"
                    : "No se puede eliminar en este momento";
            }

            return Ok(new
            {
                entityType = entityType,
                entityId = entityId,
                storeCode = storeCode,
                canDelete = canDelete,
                blockingMessage = blockingMessage,
                recommendation = canDelete 
                    ? "Operación permitida" 
                    : "Espere a que terminen las cargas activas antes de eliminar"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get paginated import history
    /// </summary>
    [HttpGet("history")]
    [Authorize(Policy = "UserOrAdmin")] // CONSULTA: Usuarios pueden ver historial de importaciones
    public async Task<ActionResult<object>> GetImportHistory(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10,
        [FromQuery] string? jobType = null,
        [FromQuery] string? status = null)
    {
        try
        {
            // Validate pagination parameters
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // Maximum page size

            var jobs = await _backgroundJobService.GetRecentJobsAsync(1000); // Get more jobs to filter and paginate
            
            // Apply filters
            var filteredJobs = jobs.AsQueryable();
            
            if (!string.IsNullOrEmpty(jobType))
            {
                filteredJobs = filteredJobs.Where(j => j.JobType.Contains(jobType, StringComparison.OrdinalIgnoreCase));
            }
            
            if (!string.IsNullOrEmpty(status))
            {
                filteredJobs = filteredJobs.Where(j => j.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
            }
            
            var totalItems = filteredJobs.Count();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            
            var pagedJobs = filteredJobs
                .OrderByDescending(j => j.StartedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            var result = pagedJobs.Select(job => new
            {
                jobId = job.JobId,
                jobType = job.JobType,
                status = job.Status,
                fileName = job.FileName,
                storeCode = job.StoreCode,
                totalRecords = job.TotalRecords,
                processedRecords = job.ProcessedRecords,
                successRecords = job.SuccessRecords,
                errorRecords = job.ErrorRecords,
                warningRecords = job.WarningRecords,
                progressPercentage = job.ProgressPercentage,
                startedAt = job.StartedAt,
                completedAt = job.CompletedAt,
                startedBy = job.StartedBy,
                errorMessage = job.ErrorMessage,
                detailedErrors = job.DetailedErrors,
                detailedWarnings = job.DetailedWarnings
            }).ToList();

            return Ok(new
            {
                data = result,
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
                    jobType = jobType,
                    status = status
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}