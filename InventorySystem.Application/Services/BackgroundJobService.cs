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
        _stockValidationService = stockValidationService;
        _batchProcessingService = batchProcessingService;
        _batchedTandiaImportService = batchedTandiaImportService;
        _excelProcessorService = excelProcessorService;
        _logger = logger;
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
            StartedAt = DateTime.UtcNow,
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
        
        // Encolar job
        Hangfire.BackgroundJob.Enqueue(() => ProcessSalesImportAsync(createdJobId!, fileData, fileName, storeCode));
        
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
            StartedAt = DateTime.UtcNow,
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
        
        Hangfire.BackgroundJob.Enqueue(() => ProcessStockImportAsync(createdJobId!, fileData, fileName, storeCode));
        
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
            StartedAt = DateTime.UtcNow,
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
        
        Hangfire.BackgroundJob.Enqueue(() => ProcessProductsImportAsync(createdJobId!, fileData, fileName));
        
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
            
            // Aquí podrías agregar la lógica de importación a la base de datos
            // Por ahora solo actualizamos el estado del job
            
            var job = await _backgroundJobRepository.GetByJobIdAsync(jobId);
            if (job != null)
            {
                job.TotalRecords = result.TotalRecords;
                job.SuccessRecords = result.SuccessCount;
                job.ErrorRecords = result.ErrorCount;
                job.WarningRecords = result.SkippedCount;
                job.ProgressPercentage = 100;
                
                if (result.Errors.Any())
                {
                    job.DetailedErrors = result.Errors;
                }
                if (result.Warnings.Any())
                {
                    job.DetailedWarnings = result.Warnings;
                }
                
                await _backgroundJobRepository.UpdateAsync(job);
            }
            
            var finalStatus = result.ErrorCount > 0 ? "COMPLETED_WITH_WARNINGS" : "COMPLETED";
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
            
            var job = await _backgroundJobRepository.GetByJobIdAsync(jobId);
            if (job != null)
            {
                job.TotalRecords = result.TotalRecords;
                job.SuccessRecords = result.SuccessCount;
                job.ErrorRecords = result.ErrorCount;
                job.WarningRecords = result.SkippedCount;
                job.ProgressPercentage = 100;
                
                if (result.Errors.Any())
                {
                    job.DetailedErrors = result.Errors;
                }
                if (result.Warnings.Any())
                {
                    job.DetailedWarnings = result.Warnings;
                }
                
                await _backgroundJobRepository.UpdateAsync(job);
            }
            
            var finalStatus = result.ErrorCount > 0 ? "COMPLETED_WITH_WARNINGS" : "COMPLETED";
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
            List<string> detailedWarnings = new List<string>();
            
            if (result.Data != null && result.Data.Any())
            {
                _logger.LogInformation($"About to persist {result.Data.Count} products for job {jobId}");
                try
                {
                    var persistenceResult = await PersistProductsDataAsync(result.Data, jobId);
                    skippedCount = persistenceResult.skippedCount;
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

    private async Task<(int skippedCount, List<string> warnings)> PersistProductsDataAsync(List<Dictionary<string, object>> productsData, string jobId)
    {
        _logger.LogInformation($"Starting to persist {productsData.Count} products for job {jobId}");
        
        // Crear o encontrar categorías, marcas, tiendas y sucursales
        var categoryCache = new Dictionary<string, Category>();
        var brandCache = new Dictionary<string, Brand>();
        var storeCache = new Dictionary<string, Store>();
        
        // Crear ImportBatch para tracking
        var importBatch = new ImportBatch
        {
            BatchCode = $"PRODUCTS-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
            BatchType = "PRODUCTS_IMPORT",
            FileName = "carga_productos.xlsx",
            TotalRecords = productsData.Count,
            SuccessCount = 0,
            SkippedCount = 0,
            ErrorCount = 0,
            ImportDate = DateTime.UtcNow,
            ImportedBy = "System",
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            IsInProgress = true,
            IsDeleted = false
        };
        
        await _importBatchRepository.AddAsync(importBatch);
        
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
        }
        
        // Actualizar ImportBatch
        importBatch.CompletedAt = DateTime.UtcNow;
        importBatch.ProcessingTimeSeconds = (DateTime.UtcNow - importBatch.StartedAt.Value).TotalSeconds;
        await _importBatchRepository.UpdateAsync(importBatch);
        
        _logger.LogInformation($"Completed persisting products for job {jobId}. Success: {importBatch.SuccessCount}, Skipped: {importBatch.SkippedCount}, Errors: {importBatch.ErrorCount}");
        
        // Preparar lista de warnings detallados
        var detailedWarnings = new List<string>();
        if (!string.IsNullOrEmpty(importBatch.Warnings))
        {
            detailedWarnings.AddRange(importBatch.Warnings.Split(new string[] { "; " }, StringSplitOptions.RemoveEmptyEntries));
        }
        
        return (importBatch.SkippedCount, detailedWarnings);
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
}