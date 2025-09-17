using InventorySystem.Application.DTOs;
using InventorySystem.Application.Interfaces;
using InventorySystem.Core.Entities;
using InventorySystem.Core.Interfaces;
using Hangfire;
using Microsoft.Extensions.Logging;
using ClosedXML.Excel;
using System.Text;

namespace InventorySystem.Application.Services;

public class BackgroundJobService : IBackgroundJobService
{
    private readonly IBackgroundJobRepository _backgroundJobRepository;
    private readonly ITandiaImportService _tandiaImportService;
    private readonly IStockInitialService _stockInitialService;
    private readonly IProductRepository _productRepository;
    private readonly IProductStockRepository _productStockRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IBrandRepository _brandRepository;
    private readonly IImportBatchRepository _importBatchRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ISaleRepository _saleRepository;
    private readonly IInventoryMovementRepository _inventoryMovementRepository;
    private readonly IStockValidationService _stockValidationService;
    private readonly BatchProcessingService _batchProcessingService;
    private readonly BatchedTandiaImportService _batchedTandiaImportService;
    private readonly ExcelProcessorService _excelProcessorService;
    private readonly ILogger<BackgroundJobService> _logger;

    public BackgroundJobService(
        IBackgroundJobRepository backgroundJobRepository,
        ITandiaImportService tandiaImportService,
        IStockInitialService stockInitialService,
        IProductRepository productRepository,
        IProductStockRepository productStockRepository,
        IStoreRepository storeRepository,
        ICategoryRepository categoryRepository,
        ISupplierRepository supplierRepository,
        IBrandRepository brandRepository,
        IImportBatchRepository importBatchRepository,
        IEmployeeRepository employeeRepository,
        ICustomerRepository customerRepository,
        ISaleRepository saleRepository,
        IInventoryMovementRepository inventoryMovementRepository,
        IStockValidationService stockValidationService,
        BatchProcessingService batchProcessingService,
        BatchedTandiaImportService batchedTandiaImportService,
        ExcelProcessorService excelProcessorService,
        ILogger<BackgroundJobService> logger)
    {
        _backgroundJobRepository = backgroundJobRepository;
        _tandiaImportService = tandiaImportService;
        _stockInitialService = stockInitialService;
        _productRepository = productRepository;
        _productStockRepository = productStockRepository;
        _storeRepository = storeRepository;
        _categoryRepository = categoryRepository;
        _supplierRepository = supplierRepository;
        _brandRepository = brandRepository;
        _importBatchRepository = importBatchRepository;
        _employeeRepository = employeeRepository;
        _customerRepository = customerRepository;
        _saleRepository = saleRepository;
        _inventoryMovementRepository = inventoryMovementRepository;
        _stockValidationService = stockValidationService;
        _batchProcessingService = batchProcessingService;
        _batchedTandiaImportService = batchedTandiaImportService;
        _excelProcessorService = excelProcessorService;
        _logger = logger;
    }

    /// <summary>
    /// Helper method to get current time in Colombia timezone (UTC-5)
    /// </summary>
    private DateTime GetColombianTime()
    {
        var colombianTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
        return TimeZoneInfo.ConvertTime(DateTime.UtcNow, colombianTimeZone);
    }

    public async Task<string> QueueSalesImportAsync(Stream excelStream, string fileName, string storeCode, string userId)
    {
        var jobId = Guid.NewGuid().ToString();
        
        // Leer archivo para obtener count de registros
        var recordCount = await GetSalesRecordCountAsync(excelStream);
        excelStream.Position = 0;
        
        // Crear registro de job usando método atómico
        var backgroundJob = new Core.Entities.BackgroundJob
        {
            JobId = jobId,
            JobType = "SALES_IMPORT",
            Status = "QUEUED",
            FileName = fileName,
            StoreCode = storeCode,
            TotalRecords = recordCount,
            ProcessedRecords = 0,
            SuccessRecords = 0,
            ErrorRecords = 0,
            WarningRecords = 0,
            ProgressPercentage = 0,
            StartedAt = GetColombianTime(),
            StartedBy = userId
        };
        
        var (success, errorMessage, createdJobId) = await _backgroundJobRepository.TryCreateJobAtomicallyAsync(backgroundJob);
        
        if (!success)
        {
            throw new InvalidOperationException(errorMessage!);
        }
        
        // Convertir stream a byte array para Hangfire
        var fileData = new byte[excelStream.Length];
        excelStream.Position = 0;
        await excelStream.ReadExactlyAsync(fileData, 0, fileData.Length);
        
        // Encolar job en Hangfire
        Hangfire.BackgroundJob.Enqueue<IBackgroundJobService>(service => service.ProcessSalesImportAsync(createdJobId!, fileData, fileName, storeCode));
        
        return createdJobId!;
    }

    public async Task<string> QueueStockImportAsync(Stream excelStream, string fileName, string storeCode, string userId)
    {
        var jobId = Guid.NewGuid().ToString();
        
        var recordCount = await GetStockRecordCountAsync(excelStream);
        excelStream.Position = 0;
        
        var backgroundJob = new Core.Entities.BackgroundJob
        {
            JobId = jobId,
            JobType = "STOCK_IMPORT",
            Status = "QUEUED",
            FileName = fileName,
            StoreCode = storeCode,
            TotalRecords = recordCount,
            ProcessedRecords = 0,
            SuccessRecords = 0,
            ErrorRecords = 0,
            WarningRecords = 0,
            ProgressPercentage = 0,
            StartedAt = GetColombianTime(),
            StartedBy = userId
        };
        
        var (success, errorMessage, createdJobId) = await _backgroundJobRepository.TryCreateJobAtomicallyAsync(backgroundJob);
        
        if (!success)
        {
            throw new InvalidOperationException(errorMessage!);
        }
        
        var fileData = new byte[excelStream.Length];
        excelStream.Position = 0;
        await excelStream.ReadExactlyAsync(fileData, 0, fileData.Length);
        
        Hangfire.BackgroundJob.Enqueue<IBackgroundJobService>(service => service.ProcessStockImportAsync(createdJobId!, fileData, fileName, storeCode));
        
        return createdJobId!;
    }

    public async Task<string> QueueProductsImportAsync(Stream excelStream, string fileName, string userId)
    {
        var jobId = Guid.NewGuid().ToString();
        
        var recordCount = await GetProductsRecordCountAsync(excelStream);
        excelStream.Position = 0;
        
        var backgroundJob = new Core.Entities.BackgroundJob
        {
            JobId = jobId,
            JobType = "PRODUCTS_IMPORT",
            Status = "QUEUED",
            FileName = fileName,
            TotalRecords = recordCount,
            ProcessedRecords = 0,
            SuccessRecords = 0,
            ErrorRecords = 0,
            WarningRecords = 0,
            ProgressPercentage = 0,
            StartedAt = GetColombianTime(),
            StartedBy = userId
        };
        
        var (success, errorMessage, createdJobId) = await _backgroundJobRepository.TryCreateJobAtomicallyAsync(backgroundJob);
        
        if (!success)
        {
            throw new InvalidOperationException(errorMessage!);
        }
        
        var fileData = new byte[excelStream.Length];
        excelStream.Position = 0;
        await excelStream.ReadExactlyAsync(fileData, 0, fileData.Length);
        
        Hangfire.BackgroundJob.Enqueue<IBackgroundJobService>(service => service.ProcessProductsImportAsync(createdJobId!, fileData, fileName));
        
        return createdJobId!;
    }

    public async Task<Core.Entities.BackgroundJob?> GetJobStatusAsync(string jobId)
    {
        return await _backgroundJobRepository.GetByJobIdAsync(jobId);
    }

    public async Task<List<Core.Entities.BackgroundJob>> GetUserJobsAsync(string userId)
    {
        return await _backgroundJobRepository.GetJobsByUserAsync(userId);
    }

    public async Task<List<Core.Entities.BackgroundJob>> GetRecentJobsAsync(int count = 10)
    {
        return await _backgroundJobRepository.GetRecentJobsAsync(count);
    }

    // Background job execution methods - Simplificados para usar ExcelProcessorService directamente
    public async Task ProcessSalesImportAsync(string jobId, byte[] fileData, string fileName, string storeCode)
    {
        try
        {
            await _backgroundJobRepository.UpdateStatusAsync(jobId, "PROCESSING");
            _logger.LogInformation($"Starting native .NET sales import job {jobId}");

            using var stream = new MemoryStream(fileData);
            
            // Usar ExcelProcessorService nativo
            var result = await _excelProcessorService.ProcessSalesAsync(stream, fileName, storeCode);
            
            // Persistir los datos de ventas en la base de datos
            bool persistenceSuccessful = true;
            string? persistenceError = null;
            int skippedCount = 0;
            int savedCount = 0;
            List<string> allWarnings = new List<string>();
            
            if (result.Data.Any())
            {
                try
                {
                    // Get user info from BackgroundJob
                    var backgroundJobForUser = await _backgroundJobRepository.GetByJobIdAsync(jobId);
                    var userId = backgroundJobForUser?.StartedBy ?? "System";
                    
                    // Try to find Employee by username for better normalization
                    var employee = await _employeeRepository.GetAllAsync();
                    var userEmployee = employee.FirstOrDefault(e => e.Name == userId || e.Code == userId);
                    int? employeeId = userEmployee?.Id;
                    
                    var (persistSkippedCount, persistWarnings, processedSavedCount, importBatchId) = await PersistSalesDataAsync(result.Data, jobId, storeCode, userId, employeeId);
                    savedCount = processedSavedCount;
                    skippedCount = persistSkippedCount;
                    allWarnings.AddRange(persistWarnings);
                    
                    // Update BackgroundJob with ImportBatchId
                    var backgroundJob = await _backgroundJobRepository.GetByJobIdAsync(jobId);
                    if (backgroundJob != null)
                    {
                        backgroundJob.ImportBatchId = importBatchId;
                        await _backgroundJobRepository.UpdateAsync(backgroundJob);
                        _logger.LogInformation($"Updated BackgroundJob {jobId} with ImportBatchId {importBatchId}");
                    }
                }
                catch (Exception ex)
                {
                    persistenceSuccessful = false;
                    persistenceError = ex.Message;
                    _logger.LogError(ex, "Error persisting sales data for job {JobId}", jobId);
                }
            }
            
            var job = await _backgroundJobRepository.GetByJobIdAsync(jobId);
            if (job != null)
            {
                job.TotalRecords = result.TotalRecords;
                job.SuccessRecords = result.SuccessCount - skippedCount;
                job.ErrorRecords = result.ErrorCount;
                job.WarningRecords = skippedCount;
                job.ProgressPercentage = 100;
                
                // Combine all errors and warnings
                var allErrors = new List<string>();
                if (result.Errors.Any())
                {
                    allErrors.AddRange(result.Errors);
                }
                if (persistenceError != null)
                {
                    allErrors.Add($"Persistence error: {persistenceError}");
                }
                if (allErrors.Any())
                {
                    job.DetailedErrors = allErrors;
                }
                
                if (allWarnings.Any())
                {
                    job.DetailedWarnings = allWarnings;
                }
                
                // Update ProcessedRecords with the actual number processed
                job.ProcessedRecords = savedCount;
                
                await _backgroundJobRepository.UpdateAsync(job);
            }
            
            var finalStatus = (result.ErrorCount == 0 && persistenceSuccessful) ? "COMPLETED" : "COMPLETED_WITH_WARNINGS";
            await _backgroundJobRepository.UpdateStatusAsync(jobId, finalStatus);
            _logger.LogInformation($"Completed native .NET sales import job {jobId} with status {finalStatus}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in sales import job {jobId}");
            await _backgroundJobRepository.UpdateStatusAsync(jobId, "FAILED", ex.Message);
        }
    }

    public async Task ProcessStockImportAsync(string jobId, byte[] fileData, string fileName, string storeCode)
    {
        try
        {
            await _backgroundJobRepository.UpdateStatusAsync(jobId, "PROCESSING");
            _logger.LogInformation($"Starting native .NET stock import job {jobId}");

            using var stream = new MemoryStream(fileData);
            
            var result = await _excelProcessorService.ProcessStockAsync(stream, fileName, storeCode);
            
            // Persist the processed data to database
            bool persistenceSuccessful = true;
            string persistenceError = null;
            
            int skippedCount = 0;
            int savedCount = 0;
            List<string> detailedWarnings = new List<string>();
            
            if (result.Data != null && result.Data.Any())
            {
                _logger.LogInformation($"About to persist {result.Data.Count} stock records for job {jobId}");
                try
                {
                    // Get user info from BackgroundJob
                    var backgroundJobForUser = await _backgroundJobRepository.GetByJobIdAsync(jobId);
                    var userId = backgroundJobForUser?.StartedBy ?? "System";
                    
                    // Try to find Employee by username for better normalization
                    var employee = await _employeeRepository.GetAllAsync();
                    var userEmployee = employee.FirstOrDefault(e => e.Name == userId || e.Code == userId);
                    int? employeeId = userEmployee?.Id;
                    
                    var persistenceResult = await PersistStockDataAsync(result.Data, jobId, storeCode, userId, employeeId);
                    skippedCount = persistenceResult.skippedCount;
                    savedCount = persistenceResult.savedCount;
                    detailedWarnings = persistenceResult.warnings;
                    _logger.LogInformation($"Successfully persisted stock data for job {jobId}. Skipped: {skippedCount}");
                }
                catch (Exception ex)
                {
                    persistenceSuccessful = false;
                    persistenceError = ex.Message;
                    _logger.LogError(ex, $"Error persisting stock data for job {jobId}: {ex.Message}");
                    // Don't throw - continue to complete the job with warnings
                }
            }
            else
            {
                _logger.LogWarning($"No stock data to persist for job {jobId}");
            }
            
            var job = await _backgroundJobRepository.GetByJobIdAsync(jobId);
            if (job != null)
            {
                job.TotalRecords = result.TotalRecords;
                job.SuccessRecords = result.SuccessCount;
                job.ErrorRecords = result.ErrorCount;
                job.WarningRecords = result.SkippedCount + skippedCount; // Include both Excel warnings and persistence warnings
                job.ProgressPercentage = 100;
                
                if (result.Errors.Any())
                {
                    job.DetailedErrors = result.Errors;
                }
                
                // Combine Excel warnings with persistence warnings
                var allWarnings = new List<string>();
                if (result.Warnings.Any())
                {
                    allWarnings.AddRange(result.Warnings);
                }
                if (detailedWarnings.Any())
                {
                    allWarnings.AddRange(detailedWarnings);
                }
                if (allWarnings.Any())
                {
                    job.DetailedWarnings = allWarnings;
                }
                
                // Add persistence error to detailed errors if it occurred
                if (!persistenceSuccessful && persistenceError != null)
                {
                    var errors = job.DetailedErrors?.ToList() ?? new List<string>();
                    errors.Add($"Error de persistencia: {persistenceError}");
                    job.DetailedErrors = errors;
                }
                
                // Update ProcessedRecords with the actual number processed
                job.ProcessedRecords = savedCount;
                
                await _backgroundJobRepository.UpdateAsync(job);
            }
            
            var finalStatus = result.ErrorCount > 0 || !persistenceSuccessful ? "COMPLETED_WITH_WARNINGS" : "COMPLETED";
            await _backgroundJobRepository.UpdateStatusAsync(jobId, finalStatus);
            _logger.LogInformation($"Completed native .NET stock import job {jobId} with status {finalStatus}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in stock import job {jobId}");
            await _backgroundJobRepository.UpdateStatusAsync(jobId, "FAILED", ex.Message);
        }
    }

    public async Task ProcessProductsImportAsync(string jobId, byte[] fileData, string fileName)
    {
        try
        {
            await _backgroundJobRepository.UpdateStatusAsync(jobId, "PROCESSING");
            _logger.LogInformation($"Starting native .NET products import job {jobId}");

            using var stream = new MemoryStream(fileData);
            
            var result = await _excelProcessorService.ProcessProductsAsync(stream, fileName);
            
            // Persistir los datos procesados en la base de datos
            bool persistenceSuccessful = true;
            string persistenceError = null;
            
            int skippedCount = 0;
            int savedCount = 0;
            List<string> detailedWarnings = new List<string>();
            
            if (result.Data != null && result.Data.Any())
            {
                _logger.LogInformation($"About to persist {result.Data.Count} products for job {jobId}");
                try
                {
                    // Get user info from BackgroundJob
                    var backgroundJobForUser = await _backgroundJobRepository.GetByJobIdAsync(jobId);
                    var userId = backgroundJobForUser?.StartedBy ?? "System";
                    
                    // Try to find Employee by username for better normalization
                    var employee = await _employeeRepository.GetAllAsync();
                    var userEmployee = employee.FirstOrDefault(e => e.Name == userId || e.Code == userId);
                    int? employeeId = userEmployee?.Id;
                    
                    var persistenceResult = await PersistProductsDataAsync(result.Data, jobId, userId, employeeId);
                    skippedCount = persistenceResult.skippedCount;
                    savedCount = persistenceResult.savedCount;
                    detailedWarnings = persistenceResult.warnings;
                    _logger.LogInformation($"Successfully persisted products for job {jobId}. Skipped: {skippedCount}");
                }
                catch (Exception ex)
                {
                    persistenceSuccessful = false;
                    persistenceError = ex.Message;
                    _logger.LogError(ex, $"Error persisting products for job {jobId}: {ex.Message}");
                    // Don't throw - continue to complete the job with warnings
                }
            }
            else
            {
                _logger.LogWarning($"No data to persist for job {jobId}");
            }
            
            var job = await _backgroundJobRepository.GetByJobIdAsync(jobId);
            if (job != null)
            {
                job.TotalRecords = result.TotalRecords;
                job.SuccessRecords = result.SuccessCount;
                job.ErrorRecords = result.ErrorCount;
                job.WarningRecords = result.SkippedCount + skippedCount; // Include both Excel warnings and persistence warnings
                job.ProgressPercentage = 100;
                
                if (result.Errors.Any())
                {
                    job.DetailedErrors = result.Errors;
                }
                
                // Combine Excel warnings with persistence warnings
                var allWarnings = new List<string>();
                if (result.Warnings.Any())
                {
                    allWarnings.AddRange(result.Warnings);
                }
                if (detailedWarnings.Any())
                {
                    allWarnings.AddRange(detailedWarnings);
                }
                if (allWarnings.Any())
                {
                    job.DetailedWarnings = allWarnings;
                }
                
                // Add persistence error to detailed errors if it occurred
                if (!persistenceSuccessful && persistenceError != null)
                {
                    var errors = job.DetailedErrors?.ToList() ?? new List<string>();
                    errors.Add($"Persistence error: {persistenceError}");
                    job.DetailedErrors = errors;
                    job.ErrorMessage = persistenceError;
                }
                
                // Update ProcessedRecords with the actual number processed
                job.ProcessedRecords = savedCount;
                
                await _backgroundJobRepository.UpdateAsync(job);
            }
            
            // Determine final status considering both processing and persistence errors
            var finalStatus = (result.ErrorCount == 0 && skippedCount == 0 && persistenceSuccessful) ? "COMPLETED" : "COMPLETED_WITH_WARNINGS";
            await _backgroundJobRepository.UpdateStatusAsync(jobId, finalStatus);
            _logger.LogInformation($"Completed native .NET products import job {jobId} with status {finalStatus}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in products import job {jobId}");
            await _backgroundJobRepository.UpdateStatusAsync(jobId, "FAILED", ex.Message);
        }
    }

    // Helper methods para contar registros
    private Task<int> GetSalesRecordCountAsync(Stream excelStream)
    {
        try
        {
            using var workbook = new XLWorkbook(excelStream);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            if (worksheet == null) return Task.FromResult(0);
            
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
            return Task.FromResult(Math.Max(0, lastRow - 1)); // -1 para excluir header
        }
        catch
        {
            return Task.FromResult(0);
        }
    }

    private Task<int> GetStockRecordCountAsync(Stream excelStream)
    {
        try
        {
            using var workbook = new XLWorkbook(excelStream);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            if (worksheet == null) return Task.FromResult(0);
            
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
            return Task.FromResult(Math.Max(0, lastRow - 1));
        }
        catch
        {
            return Task.FromResult(0);
        }
    }

    private Task<int> GetProductsRecordCountAsync(Stream excelStream)
    {
        try
        {
            using var workbook = new XLWorkbook(excelStream);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            if (worksheet == null) return Task.FromResult(0);
            
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
            return Task.FromResult(Math.Max(0, lastRow - 1));
        }
        catch
        {
            return Task.FromResult(0);
        }
    }

    private async Task<(int skippedCount, List<string> warnings, int savedCount)> PersistProductsDataAsync(List<Dictionary<string, object>> productsData, string jobId, string userId, int? employeeId)
    {
        _logger.LogInformation($"Starting to persist {productsData.Count} products for job {jobId}");
        
        // Crear o encontrar categorías, marcas, tiendas y sucursales
        var categoryCache = new Dictionary<string, Category>();
        var brandCache = new Dictionary<string, Brand>();
        var storeCache = new Dictionary<string, Store>();
        
        // Crear ImportBatch para tracking
        var startTime = GetColombianTime();
        var importBatch = new ImportBatch
        {
            BatchCode = $"PRODUCTS-{GetColombianTime():yyyyMMdd-HHmmss}",
            BatchType = "PRODUCTS_IMPORT",
            FileName = "carga_productos.xlsx",
            TotalRecords = productsData.Count,
            SuccessCount = 0,
            SkippedCount = 0,
            ErrorCount = 0,
            ImportDate = startTime,
            ImportedBy = userId,
            EmployeeId = employeeId,
            StartedAt = startTime,
            CreatedAt = startTime,
            IsInProgress = true,
            IsDeleted = false
        };
        
        await _importBatchRepository.AddAsync(importBatch);
        
        // Actualizar el BackgroundJob con el ImportBatchId para poder hacer la relación
        var backgroundJob = await _backgroundJobRepository.GetByJobIdAsync(jobId);
        if (backgroundJob != null)
        {
            backgroundJob.ImportBatchId = importBatch.Id;
            await _backgroundJobRepository.UpdateAsync(backgroundJob);
        }

        int processedProducts = 0;
        int totalProducts = productsData.Count;

        foreach (var productData in productsData)
        {
            try
            {
                // 1. Crear/obtener categoría
                var categoryName = productData["CategoryName"]?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(categoryName))
                {
                    if (!categoryCache.ContainsKey(categoryName))
                    {
                        var existingCategory = await _categoryRepository.GetByNameAsync(categoryName);
                        if (existingCategory == null)
                        {
                            var newCategory = new Category 
                            { 
                                Name = categoryName, 
                                Description = $"Categoría creada automáticamente: {categoryName}",
                                Active = true,
                                CreatedAt = DateTime.UtcNow
                            };
                            existingCategory = await _categoryRepository.AddAsync(newCategory);
                            _logger.LogInformation($"Created new category: {categoryName}");
                        }
                        categoryCache[categoryName] = existingCategory;
                    }
                }
                
                // 2. Crear/obtener marca
                var brandName = productData["SupplierName"]?.ToString()?.Trim();
                Brand? brand = null;
                if (!string.IsNullOrEmpty(brandName))
                {
                    if (!brandCache.ContainsKey(brandName))
                    {
                        var existingBrand = await _brandRepository.GetByNameAsync(brandName);
                        if (existingBrand == null)
                        {
                            var newBrand = new Brand 
                            { 
                                Name = brandName,
                                Description = $"Marca creada automáticamente: {brandName}",
                                Active = true,
                                CreatedAt = DateTime.UtcNow
                            };
                            existingBrand = await _brandRepository.AddAsync(newBrand);
                            _logger.LogInformation($"Created new brand: {brandName}");
                        }
                        brandCache[brandName] = existingBrand;
                    }
                    brand = brandCache[brandName];
                }
                
                // 3. Crear/obtener tienda
                var storeName = productData["StoreName"]?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(storeName))
                {
                    if (!storeCache.ContainsKey(storeName))
                    {
                        var existingStore = await _storeRepository.GetByNameAsync(storeName);
                        if (existingStore == null)
                        {
                            var storeCode = GenerateStoreCode(storeName);
                            var newStore = new Store 
                            { 
                                Code = storeCode,
                                Name = storeName,
                                Address = $"Tienda creada automáticamente: {storeName}",
                                Phone = "N/A",
                                Active = true,
                                CreatedAt = DateTime.UtcNow
                            };
                            existingStore = await _storeRepository.AddAsync(newStore);
                            _logger.LogInformation($"Created new store: {storeName}");
                        }
                        storeCache[storeName] = existingStore;
                    }
                }
                
                // 4. Crear producto
                var productCode = productData["Code"]?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(productCode))
                {
                    var existingProduct = await _productRepository.GetByCodeAsync(productCode);
                    if (existingProduct == null)
                    {
                        var product = new Product
                        {
                            Code = productCode,
                            Name = productData["Name"]?.ToString() ?? "",
                            Description = productData["Description"]?.ToString(),
                            PurchasePrice = Convert.ToDecimal(productData["PurchasePrice"]),
                            SalePrice = Convert.ToDecimal(productData["SalePrice"]),
                            Stock = Convert.ToDecimal(productData["Stock"]),
                            MinimumStock = Convert.ToDecimal(productData["MinimumStock"]),
                            Unit = productData["Unit"]?.ToString(),
                            Active = Convert.ToBoolean(productData["Active"]),
                            CategoryId = (!string.IsNullOrEmpty(categoryName) && categoryCache.ContainsKey(categoryName)) 
                                ? categoryCache[categoryName].Id 
                                : categoryCache.Values.FirstOrDefault()?.Id ?? 1,
                            BrandId = brand?.Id,
                            ImportBatchId = importBatch.Id,
                            CreatedAt = DateTime.UtcNow
                        };
                        
                        await _productRepository.AddAsync(product);
                        _logger.LogDebug($"Created product: {productCode} - {product.Name}");
                        
                        // Create corresponding ProductStock record for this product in the store
                        var store = storeCache.Values.FirstOrDefault(); // Get the store from the cache
                        if (store != null)
                        {
                            var productStock = new ProductStock
                            {
                                ProductId = product.Id,
                                StoreId = store.Id,
                                CurrentStock = 0,// Stock 0
                                MinimumStock = Convert.ToDecimal(productData["MinimumStock"]),
                                MaximumStock = Convert.ToDecimal(productData["MinimumStock"]) * 3, // Default to 3x minimum
                                AverageCost = Convert.ToDecimal(productData["PurchasePrice"]),
                                ImportBatchId = importBatch.Id,
                                CreatedAt = DateTime.UtcNow,
                                IsDeleted = false
                            };
                            
                            await _productStockRepository.AddAsync(productStock);
                            _logger.LogDebug($"Created ProductStock for product: {productCode} in store: {store.Code}");
                        }
                        
                        importBatch.SuccessCount++;
                    }
                    else
                    {
                        _logger.LogWarning($"Product with code {productCode} already exists, skipping");
                        importBatch.SkippedCount++;
                        
                        var warningMessage = $"Producto {productCode} ya existe en la base de datos";
                        if (string.IsNullOrEmpty(importBatch.Warnings))
                            importBatch.Warnings = warningMessage;
                        else
                            importBatch.Warnings += "; " + warningMessage;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error persisting product data: {ex.Message}");
                importBatch.ErrorCount++;

                var errorMessage = $"Error en producto {productData["Code"]}: {ex.Message}";
                if (string.IsNullOrEmpty(importBatch.Errors))
                    importBatch.Errors = errorMessage;
                else
                    importBatch.Errors += "; " + errorMessage;
            }

            // Update progress every 10 processed products or if we're at the end
            processedProducts++;
            if (processedProducts % 10 == 0 || processedProducts == totalProducts)
            {
                try
                {
                    var job = await _backgroundJobRepository.GetByJobIdAsync(jobId);
                    if (job != null)
                    {
                        job.ProcessedRecords = processedProducts;
                        job.ProgressPercentage = (int)Math.Round((double)processedProducts / totalProducts * 100);
                        await _backgroundJobRepository.UpdateAsync(job);
                        _logger.LogInformation($"Progress updated for job {jobId}: {processedProducts}/{totalProducts} ({job.ProgressPercentage}%)");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to update progress for job {JobId}", jobId);
                }
            }
        }
        
        // Actualizar ImportBatch
        var completedTime = GetColombianTime();
        importBatch.CompletedAt = completedTime;
        importBatch.ProcessingTimeSeconds = (completedTime - startTime).TotalSeconds;
        importBatch.IsInProgress = false;
        await _importBatchRepository.UpdateAsync(importBatch);
        
        _logger.LogInformation($"Completed persisting products for job {jobId}. Success: {importBatch.SuccessCount}, Skipped: {importBatch.SkippedCount}, Errors: {importBatch.ErrorCount}");
        
        // Preparar lista de warnings detallados
        var detailedWarnings = new List<string>();
        if (!string.IsNullOrEmpty(importBatch.Warnings))
        {
            detailedWarnings.AddRange(importBatch.Warnings.Split(new string[] { "; " }, StringSplitOptions.RemoveEmptyEntries));
        }
        
        return (importBatch.SkippedCount, detailedWarnings, importBatch.SuccessCount);
    }

    private string GenerateStoreCode(string storeName)
    {
        // Generar un código de máximo 10 caracteres
        if (string.IsNullOrEmpty(storeName))
            return "STORE01";
        
        // Tomar las primeras letras de cada palabra
        var words = storeName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var codeBuilder = new StringBuilder();
        
        foreach (var word in words)
        {
            if (codeBuilder.Length < 8) // Dejar espacio para números
            {
                codeBuilder.Append(word.Substring(0, Math.Min(word.Length, 3)).ToUpper());
            }
        }
        
        // Si es muy corto, agregar parte del primer nombre
        if (codeBuilder.Length < 5 && words.Length > 0)
        {
            var firstWord = words[0].ToUpper();
            codeBuilder.Clear();
            codeBuilder.Append(firstWord.Substring(0, Math.Min(firstWord.Length, 5)));
        }
        
        // Asegurar que no sea más de 8 caracteres (dejamos 2 para números si es necesario)
        var baseCode = codeBuilder.ToString();
        if (baseCode.Length > 8)
            baseCode = baseCode.Substring(0, 8);
        
        // Si está vacío, usar valor por defecto
        if (string.IsNullOrEmpty(baseCode))
            baseCode = "STORE";
        
        return baseCode;
    }

    private async Task<(int skippedCount, List<string> warnings, int savedCount)> PersistStockDataAsync(List<Dictionary<string, object>> stockData, string jobId, string storeCode, string userId, int? employeeId)
    {
        _logger.LogInformation($"Starting to persist {stockData.Count} stock records for job {jobId} in store {storeCode}");
        
        var warnings = new List<string>();
        int skippedCount = 0;
        int savedCount = 0;

        // Get store entity
        var store = await _storeRepository.GetByCodeAsync(storeCode);
        if (store == null)
        {
            warnings.Add($"Store with code {storeCode} not found");
            return (stockData.Count, warnings, 0);
        }

        // Get existing products
        var products = await _productRepository.GetAllAsync();
        var productsDict = products
            .Where(p => !string.IsNullOrEmpty(p.Code))
            .GroupBy(p => p.Code)
            .ToDictionary(g => g.Key, g => g.First());

        // Create ImportBatch for tracking
        var startTime = GetColombianTime();
        var importBatch = new ImportBatch
        {
            BatchCode = $"STOCK-{GetColombianTime():yyyyMMdd-HHmmss}",
            BatchType = "STOCK_IMPORT",
            FileName = "carga_stock.xlsx",
            StoreCode = storeCode,
            TotalRecords = stockData.Count,
            SuccessCount = 0,
            SkippedCount = 0,
            ErrorCount = 0,
            ImportDate = startTime,
            ImportedBy = userId,
            EmployeeId = employeeId,
            StartedAt = startTime,
            CreatedAt = startTime,
            IsInProgress = true,
            IsDeleted = false
        };

        await _importBatchRepository.AddAsync(importBatch);

        // Update the BackgroundJob with the ImportBatchId
        var backgroundJob = await _backgroundJobRepository.GetByJobIdAsync(jobId);
        if (backgroundJob != null)
        {
            backgroundJob.ImportBatchId = importBatch.Id;
            await _backgroundJobRepository.UpdateAsync(backgroundJob);
        }

        int processedStockRecords = 0;
        int totalStockRecords = stockData.Count;

        foreach (var stockRecord in stockData)
        {
            try
            {
                var productCode = stockRecord["codigo"]?.ToString()?.Trim();
                if (string.IsNullOrEmpty(productCode))
                {
                    warnings.Add("Product code is empty, skipping record");
                    skippedCount++;
                    continue;
                }

                if (!productsDict.ContainsKey(productCode))
                {
                    warnings.Add($"Product {productCode} not found in system");
                    skippedCount++;
                    continue;
                }

                var product = productsDict[productCode];

                // Check if stock already exists for this product/store
                var existingStock = await _productStockRepository.GetByProductAndStoreAsync(product.Id, store.Id);

                var currentStock = Convert.ToDecimal(stockRecord["current_stock"] ?? 0);
                var minimumStock = Convert.ToDecimal(stockRecord["minimum_stock"] ?? 0);
                var maximumStock = Convert.ToDecimal(stockRecord["maximum_stock"] ?? minimumStock * 3);

                ProductStock productStock;
                
                if (existingStock != null)
                {
                    // Update existing ProductStock
                    productStock = existingStock;
                    productStock.CurrentStock = currentStock;
                    productStock.MinimumStock = minimumStock;
                    productStock.MaximumStock = maximumStock;
                    productStock.AverageCost = product.PurchasePrice;
                    productStock.ImportBatchId = importBatch.Id;
                    productStock.UpdatedAt = DateTime.UtcNow;
                    
                    await _productStockRepository.UpdateAsync(productStock);
                    warnings.Add($"Updated existing stock for product {productCode} in store {storeCode}");
                }
                else
                {
                    // Create new ProductStock
                    productStock = new ProductStock
                    {
                        ProductId = product.Id,
                        StoreId = store.Id,
                        CurrentStock = currentStock,
                        MinimumStock = minimumStock,
                        MaximumStock = maximumStock,
                        AverageCost = product.PurchasePrice,
                        ImportBatchId = importBatch.Id,
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };

                    await _productStockRepository.AddAsync(productStock);
                }
                
                savedCount++;
            }
            catch (Exception ex)
            {
                warnings.Add($"Error processing stock record for product {stockRecord.GetValueOrDefault("codigo", "Unknown")}: {ex.Message}");
                skippedCount++;
                _logger.LogError(ex, "Error processing stock record for job {JobId}", jobId);
            }

            // Update progress every 10 processed stock records or if we're at the end
            processedStockRecords++;
            if (processedStockRecords % 10 == 0 || processedStockRecords == totalStockRecords)
            {
                try
                {
                    var job = await _backgroundJobRepository.GetByJobIdAsync(jobId);
                    if (job != null)
                    {
                        job.ProcessedRecords = processedStockRecords;
                        job.ProgressPercentage = (int)Math.Round((double)processedStockRecords / totalStockRecords * 100);
                        await _backgroundJobRepository.UpdateAsync(job);
                        _logger.LogInformation($"Progress updated for job {jobId}: {processedStockRecords}/{totalStockRecords} ({job.ProgressPercentage}%)");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to update progress for job {JobId}", jobId);
                }
            }
        }

        // Update ImportBatch with final results
        var completedTime = GetColombianTime();
        importBatch.SuccessCount = savedCount;
        importBatch.SkippedCount = skippedCount;
        importBatch.ErrorCount = warnings.Count;
        importBatch.CompletedAt = completedTime;
        importBatch.ProcessingTimeSeconds = (completedTime - startTime).TotalSeconds;
        importBatch.IsInProgress = false;
        await _importBatchRepository.UpdateAsync(importBatch);

        _logger.LogInformation($"Completed persisting stock data for job {jobId}. Saved: {savedCount}, Skipped: {skippedCount}");

        // Marcar la tienda como que ya tiene stock inicial cargado
        if (savedCount > 0) // Solo si se guardaron registros exitosamente
        {
            try
            {
                store.HasInitialStock = true;
                await _storeRepository.UpdateAsync(store);
                _logger.LogInformation($"Marked store {storeCode} as having initial stock loaded");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to mark store {storeCode} as having initial stock, but import was successful");
            }
        }

        return (skippedCount, warnings, savedCount);
    }

    private async Task<(int skippedCount, List<string> warnings, int savedCount, int importBatchId)> PersistSalesDataAsync(List<Dictionary<string, object>> salesData, string jobId, string storeCode, string userId, int? employeeId)
    {
        _logger.LogInformation($"Starting to persist {salesData.Count} sale detail lines for job {jobId}");

        var warnings = new List<string>();
        int skippedCount = 0;
        int savedCount = 0;

        // Get store entity
        var store = await _storeRepository.GetByCodeAsync(storeCode);
        if (store == null)
        {
            throw new InvalidOperationException($"Store not found: {storeCode}");
        }

        // Caches for entities
        var employeeCache = new Dictionary<string, Employee>();
        var customerCache = new Dictionary<string, Customer>();
        var productCache = new Dictionary<string, Product>();

        // Create ImportBatch for tracking
        var startTime = GetColombianTime();
        var importBatch = new ImportBatch
        {
            BatchCode = $"SALES-{GetColombianTime():yyyyMMdd-HHmmss}",
            BatchType = "SALES",
            FileName = $"sales_import_{jobId}.xlsx",
            StoreCode = storeCode,
            TotalRecords = salesData.Count,
            ImportDate = startTime,
            ImportedBy = userId,
            EmployeeId = employeeId,
            StartedAt = startTime,
            CreatedAt = startTime,
            IsInProgress = true
        };

        await _importBatchRepository.AddAsync(importBatch);

        // Group sales data by SaleNumber (each sale number is one sale with multiple products)
        var salesGrouped = salesData.GroupBy(x => x.GetValueOrDefault("SaleNumber", "")?.ToString()?.Trim())
            .Where(g => !string.IsNullOrEmpty(g.Key))
            .ToList();

        _logger.LogInformation($"Found {salesGrouped.Count} unique sales documents");

        var productStocks = await _productStockRepository.GetByStoreIdAsync(store.Id);
        int processedSales = 0;
        int totalSalesGroups = salesGrouped.Count();

        foreach (var saleGroup in salesGrouped)
        {
            try
            {
                var saleNumber = saleGroup.Key; // This is now the SaleNumber from column F
                var saleDetails = saleGroup.ToList();
                var firstRecord = saleDetails.First(); // Use first record for sale-level data
                var documentNumber = firstRecord.GetValueOrDefault("DocumentNumber", "")?.ToString()?.Trim(); // Keep original document number for reference
                
                // Create or find Employee (same for all items in this sale)
                var employeeName = firstRecord.GetValueOrDefault("EmployeeName", "")?.ToString()?.Trim();
                Employee? employee = null;
                
                if (!string.IsNullOrEmpty(employeeName))
                {
                    if (!employeeCache.ContainsKey(employeeName))
                    {
                        employee = await _employeeRepository.GetByNameAsync(employeeName);
                        if (employee == null)
                        {
                            // Create new employee
                            employee = new Employee
                            {
                                Code = employeeName.Replace(" ", "").ToUpper(),
                                Name = employeeName,
                                StoreId = store.Id,
                                Active = true,
                                HireDate = DateTime.UtcNow,
                                CreatedAt = DateTime.UtcNow
                            };
                            await _employeeRepository.AddAsync(employee);
                        }
                        employeeCache[employeeName] = employee;
                    }
                    else
                    {
                        employee = employeeCache[employeeName];
                    }
                }

                // Create or find Customer (same for all items in this sale)
                var customerName = firstRecord.GetValueOrDefault("CustomerName", "")?.ToString()?.Trim();
                Customer? customer = null;

                if (!string.IsNullOrEmpty(customerName))
                {
                    var customerKey = customerName;
                    if (!customerCache.ContainsKey(customerKey))
                    {
                        customer = await _customerRepository.GetByNameAsync(customerName);

                        if (customer == null)
                        {
                            // Create new customer using name from column D
                            customer = new Customer
                            {
                                Name = customerName,
                                Document = "99999999", // Default document for generic customers
                                Active = true,
                                CreatedAt = DateTime.UtcNow
                            };
                            await _customerRepository.AddAsync(customer);
                        }
                        customerCache[customerKey] = customer;
                    }
                    else
                    {
                        customer = customerCache[customerKey];
                    }
                }

                // saleNumber already comes from the grouping key (column F)
                // No need to read it again from firstRecord since we're already grouped by it
                var saleDate = (DateTime)firstRecord.GetValueOrDefault("SaleDate", GetColombianTime());
                var total = saleDetails.Sum(x => Convert.ToDecimal(x.GetValueOrDefault("Subtotal", 0)));

                if (string.IsNullOrEmpty(saleNumber))
                {
                    warnings.Add($"Sale record skipped: Missing sale number");
                    skippedCount++;
                    continue;
                }

                // Log employee assignment for debugging
                _logger.LogInformation($"Creating sale {saleNumber}. Employee: {employeeName} -> ID: {employee?.Id}, Customer: {customerName} -> ID: {customer?.Id}");

                var sale = new Sale
                {
                    SaleNumber = saleNumber,
                    SaleDate = saleDate,
                    Total = total,
                    SubTotal = total, // Assuming no tax separation in import
                    Taxes = 0,
                    StoreId = store.Id,
                    CustomerId = customer?.Id,
                    EmployeeId = employee?.Id,
                    ImportBatchId = importBatch.Id,
                    ImportedAt = DateTime.UtcNow,
                    ImportSource = "TANDIA_EXCEL",
                    CreatedAt = DateTime.UtcNow
                };

                // Initialize the Details collection
                sale.Details = new List<SaleDetail>();

                // Collect sale details and inventory movements to create after saving the sale
                var saleDetailsToAdd = new List<SaleDetail>();
                var inventoryMovementsToAdd = new List<InventoryMovement>();

                // Create SaleDetails for each product in this sale
                foreach (var detail in saleDetails)
                {
                    var productName = detail.GetValueOrDefault("ProductName", "")?.ToString()?.Trim();
                    var quantity = Convert.ToDecimal(detail.GetValueOrDefault("Quantity", 1));
                    var unitPrice = Convert.ToDecimal(detail.GetValueOrDefault("UnitPrice", 0));
                    var subtotal = Convert.ToDecimal(detail.GetValueOrDefault("Subtotal", 0));

                    if (string.IsNullOrEmpty(productName))
                    {
                        warnings.Add($"Product name missing in sale {saleNumber}");
                        continue;
                    }

                    // Find or cache product
                    Product? product = null;
                    if (!productCache.ContainsKey(productName))
                    {
                        var searchResults = await _productRepository.SearchProductsAsync(productName);
                        product = searchResults.FirstOrDefault(p => p.Name.Equals(productName, StringComparison.OrdinalIgnoreCase));
                        if (product == null)
                        {
                            // Use first available product as fallback
                            product = (await _productRepository.GetAllAsync()).FirstOrDefault();
                            if (product != null)
                            {
                                warnings.Add($"Product '{productName}' not found, using fallback product '{product.Name}'");
                            }
                        }
                        if (product != null)
                        {
                            productCache[productName] = product;
                        }
                    }
                    else
                    {
                        product = productCache[productName];
                    }

                    if (product != null)
                    {
                        // Create SaleDetail (will be added after sale is saved)
                        var saleDetail = new SaleDetail
                        {
                            ProductId = product.Id,
                            Quantity = quantity,
                            UnitPrice = unitPrice,
                            Subtotal = subtotal,
                            CreatedAt = DateTime.UtcNow
                        };

                        // Add to temporary collection
                        saleDetailsToAdd.Add(saleDetail);

                        // Prepare inventory movement for this product (will be added after sale is saved)
                        var inventoryMovement = new InventoryMovement
                        {
                            Type = MovementType.TandiaImport_Sale,
                            Date = saleDate,
                            Quantity = -quantity,
                            Reason = $"Sale: {saleNumber} - {productName}",
                            DocumentNumber = saleNumber,
                            Source = "TANDIA_EXCEL",
                            UnitCost = unitPrice,
                            TotalCost = subtotal,
                            ProductId = product.Id,
                            StoreId = store.Id,
                            CreatedAt = DateTime.UtcNow
                        };

                        // Find specific product stock and reduce it
                        var productStock = productStocks.FirstOrDefault(ps => ps.ProductId == product.Id);
                        if (productStock != null && productStock.CurrentStock >= quantity)
                        {
                            var previousStock = productStock.CurrentStock;
                            productStock.CurrentStock -= quantity;
                            productStock.UpdatedAt = DateTime.UtcNow;
                            
                            await _productStockRepository.UpdateAsync(productStock);

                            // Update the inventory movement with actual stock values
                            inventoryMovement.PreviousStock = previousStock;
                            inventoryMovement.NewStock = productStock.CurrentStock;
                            inventoryMovement.ProductStockId = productStock.Id;
                        }
                        else if (productStock != null)
                        {
                            // Insufficient stock - reduce what's available
                            var previousStock = productStock.CurrentStock;
                            var availableQuantity = productStock.CurrentStock;
                            productStock.CurrentStock = 0;
                            productStock.UpdatedAt = DateTime.UtcNow;
                            
                            await _productStockRepository.UpdateAsync(productStock);

                            inventoryMovement.PreviousStock = previousStock;
                            inventoryMovement.NewStock = 0;
                            inventoryMovement.Quantity = -availableQuantity;
                            inventoryMovement.ProductStockId = productStock.Id;
                            
                            warnings.Add($"Insufficient stock for product '{productName}'. Required: {quantity}, Available: {availableQuantity}");
                        }

                        // Add to temporary collection
                        inventoryMovementsToAdd.Add(inventoryMovement);
                    }
                    else
                    {
                        warnings.Add($"Product '{productName}' not found for sale {saleNumber}");
                    }
                }

                // Save the sale first to get the generated ID
                await _saleRepository.AddAsync(sale);
                _logger.LogInformation($"Sale {saleNumber} saved with ID: {sale.Id}, EmployeeId: {sale.EmployeeId}");

                // Now that sale has an ID, add the SaleDetails with correct SaleId
                foreach (var saleDetail in saleDetailsToAdd)
                {
                    saleDetail.SaleId = sale.Id;
                    sale.Details.Add(saleDetail);
                }

                // Add inventory movements with correct SaleId
                foreach (var inventoryMovement in inventoryMovementsToAdd)
                {
                    inventoryMovement.SaleId = sale.Id;
                    await _inventoryMovementRepository.AddAsync(inventoryMovement);
                }

                savedCount++;
            }
            catch (Exception ex)
            {
                warnings.Add($"Error processing sale group {saleGroup.Key}: {ex.Message}");
                skippedCount++;
                _logger.LogError(ex, "Error processing sale group {SaleNumber} for job {JobId}", saleGroup.Key, jobId);
            }

            // Update progress every 10 processed sales or if we're at the end
            processedSales++;
            if (processedSales % 10 == 0 || processedSales == totalSalesGroups)
            {
                try
                {
                    var job = await _backgroundJobRepository.GetByJobIdAsync(jobId);
                    if (job != null)
                    {
                        job.ProcessedRecords = processedSales;
                        job.ProgressPercentage = (int)Math.Round((double)processedSales / totalSalesGroups * 100);
                        await _backgroundJobRepository.UpdateAsync(job);
                        _logger.LogInformation($"Progress updated for job {jobId}: {processedSales}/{totalSalesGroups} ({job.ProgressPercentage}%)");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to update progress for job {JobId}", jobId);
                }
            }
        }

        // Update ImportBatch with final results
        var completedTime = GetColombianTime();
        importBatch.SuccessCount = savedCount;
        importBatch.SkippedCount = skippedCount;
        importBatch.ErrorCount = warnings.Count;
        importBatch.CompletedAt = completedTime;
        importBatch.ProcessingTimeSeconds = (completedTime - startTime).TotalSeconds;
        importBatch.IsInProgress = false;
        
        // Format warnings as JSON like FastImportsController
        if (warnings.Count > 0)
        {
            importBatch.Warnings = System.Text.Json.JsonSerializer.Serialize(warnings);
        }
        
        await _importBatchRepository.UpdateAsync(importBatch);

        _logger.LogInformation($"Completed persisting sales data for job {jobId}. Saved: {savedCount}, Skipped: {skippedCount}");

        return (skippedCount, warnings, savedCount, importBatch.Id);
    }
}