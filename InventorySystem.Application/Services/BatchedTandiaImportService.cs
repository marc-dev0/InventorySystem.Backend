using InventorySystem.Application.DTOs;
using InventorySystem.Application.Interfaces;
using InventorySystem.Core.Entities;
using InventorySystem.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace InventorySystem.Application.Services;

public class BatchedTandiaImportService
{
    private readonly ITandiaImportService _tandiaImportService;
    private readonly IProductRepository _productRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ISaleRepository _saleRepository;
    private readonly IInventoryMovementRepository _inventoryMovementRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IProductStockRepository _productStockRepository;
    private readonly IImportBatchRepository _importBatchRepository;
    private readonly BatchProcessingService _batchProcessingService;
    private readonly ILogger<BatchedTandiaImportService> _logger;

    public BatchedTandiaImportService(
        ITandiaImportService tandiaImportService,
        IProductRepository productRepository,
        ICustomerRepository customerRepository,
        ISaleRepository saleRepository,
        IInventoryMovementRepository inventoryMovementRepository,
        IStoreRepository storeRepository,
        IProductStockRepository productStockRepository,
        IImportBatchRepository importBatchRepository,
        BatchProcessingService batchProcessingService,
        ILogger<BatchedTandiaImportService> logger)
    {
        _tandiaImportService = tandiaImportService;
        _productRepository = productRepository;
        _customerRepository = customerRepository;
        _saleRepository = saleRepository;
        _inventoryMovementRepository = inventoryMovementRepository;
        _storeRepository = storeRepository;
        _productStockRepository = productStockRepository;
        _importBatchRepository = importBatchRepository;
        _batchProcessingService = batchProcessingService;
        _logger = logger;
    }

    /// <summary>
    /// Importa ventas usando procesamiento por lotes optimizado
    /// </summary>
    public async Task<BulkUploadResultDto> ImportSalesInBatchesAsync(
        string jobId, 
        List<TandiaSaleDetailDto> salesData, 
        string storeCode,
        string fileName)
    {
        var startTime = DateTime.Now;
        var result = new BulkUploadResultDto
        {
            TotalRecords = salesData.Count
        };

        try
        {
            // Verificar que existe la sucursal
            var store = await _storeRepository.FirstOrDefaultAsync(s => s.Code == storeCode);
            if (store == null)
            {
                result.Errors.Add($"Sucursal con código '{storeCode}' no encontrada");
                result.ErrorCount++;
                return result;
            }

            // Crear ImportBatch
            var batchCode = await GenerateUniqueBatchCodeAsync("SALES");
            var importBatch = new ImportBatch
            {
                BatchCode = batchCode,
                BatchType = "SALES",
                FileName = fileName,
                StoreCode = storeCode,
                ImportDate = DateTime.UtcNow,
                ImportedBy = "SYSTEM",
                TotalRecords = salesData.Count,
                SuccessCount = 0,
                SkippedCount = 0,
                ErrorCount = 0
            };

            await _importBatchRepository.AddAsync(importBatch);

            // Agrupar ventas por documento para procesamiento eficiente
            var salesGroups = salesData
                .GroupBy(s => s.DocumentNumber)
                .Select(g => new SalesDocumentBatch
                {
                    DocumentNumber = g.Key,
                    SaleDetails = g.ToList(),
                    CustomerName = g.First().CustomerName,
                    CustomerDocument = g.First().CustomerDocument,
                    SaleDate = g.First().Date
                })
                .ToList();

            // Calcular tamaño de lote óptimo
            var batchSize = _batchProcessingService.CalculateOptimalBatchSize(salesGroups.Count);
            
            _logger.LogInformation($"Job {jobId}: Processing {salesGroups.Count} sales documents in batches of {batchSize}");

            // Procesar en lotes
            var batchResult = await _batchProcessingService.ProcessInBatchesAsync(
                jobId,
                salesGroups,
                async (batch) => await ProcessSalesBatch(batch, store, importBatch),
                batchSize
            );

            // Actualizar estadísticas finales
            result.SuccessCount = batchResult.SuccessRecords;
            result.ErrorCount = batchResult.ErrorRecords;
            result.SkippedCount = batchResult.WarningRecords;

            // Actualizar ImportBatch
            importBatch.SuccessCount = result.SuccessCount;
            importBatch.ErrorCount = result.ErrorCount;
            importBatch.SkippedCount = result.SkippedCount;
            await _importBatchRepository.UpdateAsync(importBatch);

            result.ProcessingTime = DateTime.Now - startTime;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Job {jobId}: Error in batched sales import");
            result.Errors.Add($"Error crítico: {ex.Message}");
            result.ErrorCount = salesData.Count;
            result.ProcessingTime = DateTime.Now - startTime;
            return result;
        }
    }

    /// <summary>
    /// Procesa un lote de documentos de venta
    /// </summary>
    private async Task<BatchResultDto> ProcessSalesBatch(
        List<SalesDocumentBatch> salesBatch, 
        Store store, 
        ImportBatch importBatch)
    {
        var batchResult = new BatchResultDto
        {
            ProcessedCount = salesBatch.Count
        };
        var batchStartTime = DateTime.UtcNow;

        try
        {
            foreach (var salesDocument in salesBatch)
            {
                try
                {
                    // Verificar si la venta ya existe
                    var existingSale = await _saleRepository.FirstOrDefaultAsync(s => s.SaleNumber == salesDocument.DocumentNumber);
                    if (existingSale != null)
                    {
                        batchResult.WarningCount++;
                        batchResult.Warnings.Add($"Sale {salesDocument.DocumentNumber} already exists, skipping");
                        continue;
                    }

                    // Obtener o crear cliente
                    var customer = await GetOrCreateCustomerAsync(salesDocument.CustomerName, salesDocument.CustomerDocument);

                    // Crear venta
                    var sale = new Sale
                    {
                        SaleNumber = salesDocument.DocumentNumber,
                        SaleDate = DateTime.SpecifyKind(salesDocument.SaleDate, DateTimeKind.Utc),
                        CustomerId = customer?.Id,
                        StoreId = store.Id,
                        SubTotal = 0,
                        Taxes = 0,
                        Total = 0,
                        ImportedAt = DateTime.UtcNow,
                        ImportSource = importBatch.BatchCode,
                        ImportBatchId = importBatch.Id,
                        Details = new List<SaleDetail>()
                    };

                    decimal saleSubTotal = 0;
                    decimal saleTaxes = 0;
                    var movementsToCreate = new List<InventoryMovement>();

                    // Procesar detalles de la venta
                    foreach (var saleDetail in salesDocument.SaleDetails)
                    {
                        var product = await _productRepository.FirstOrDefaultAsync(p => p.Code == saleDetail.ProductCode);
                        if (product == null)
                        {
                            batchResult.Errors.Add($"Product {saleDetail.ProductCode} not found in document {salesDocument.DocumentNumber}");
                            continue;
                        }

                        var detail = new SaleDetail
                        {
                            ProductId = product.Id,
                            Quantity = saleDetail.Quantity,
                            UnitPrice = saleDetail.SalePrice,
                            Subtotal = saleDetail.Total
                        };

                        sale.Details.Add(detail);
                        saleSubTotal += saleDetail.Total;
                    }

                    // Actualizar totales de la venta
                    sale.SubTotal = saleSubTotal;
                    sale.Total = saleSubTotal + saleTaxes;

                    // Guardar venta
                    await _saleRepository.AddAsync(sale);

                    // Procesar movimientos de inventario
                    foreach (var saleDetail in salesDocument.SaleDetails)
                    {
                        var product = await _productRepository.FirstOrDefaultAsync(p => p.Code == saleDetail.ProductCode);
                        if (product == null) continue;

                        var productStock = await GetOrCreateProductStockAsync(product.Id, store.Id);
                        
                        // Actualizar stock
                        var previousStock = productStock.CurrentStock;
                        productStock.CurrentStock -= saleDetail.Quantity;
                        await _productStockRepository.UpdateAsync(productStock);

                        // Crear movimiento de inventario
                        var movement = new InventoryMovement
                        {
                            Date = DateTime.SpecifyKind(salesDocument.SaleDate, DateTimeKind.Utc),
                            Type = MovementType.Sale,
                            Quantity = -(int)saleDetail.Quantity,
                            Reason = $"Sale imported from batch - {salesDocument.DocumentNumber}",
                            PreviousStock = (int)previousStock,
                            NewStock = (int)productStock.CurrentStock,
                            DocumentNumber = salesDocument.DocumentNumber,
                            UserName = "Batch_Import",
                            Source = "Batch_Import",
                            UnitCost = saleDetail.SalePrice,
                            TotalCost = saleDetail.Total,
                            ProductId = product.Id,
                            StoreId = store.Id,
                            ProductStockId = productStock.Id,
                            SaleId = sale.Id
                        };

                        await _inventoryMovementRepository.AddAsync(movement);
                    }

                    batchResult.SuccessCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing sales document {salesDocument.DocumentNumber}");
                    batchResult.ErrorCount++;
                    batchResult.Errors.Add($"Error processing document {salesDocument.DocumentNumber}: {ex.Message}");
                }
            }

            batchResult.ProcessingTime = DateTime.UtcNow - batchStartTime;
            return batchResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing sales batch");
            batchResult.ErrorCount = salesBatch.Count;
            batchResult.Errors.Add($"Batch processing error: {ex.Message}");
            batchResult.ProcessingTime = DateTime.UtcNow - batchStartTime;
            return batchResult;
        }
    }

    // Helper methods
    private async Task<Customer?> GetOrCreateCustomerAsync(string customerName, string? customerDocument)
    {
        if (string.IsNullOrEmpty(customerName) || customerName == "Cliente Genérico")
            return null;

        var existingCustomer = await _customerRepository.FirstOrDefaultAsync(c => c.Name == customerName);
        if (existingCustomer != null)
            return existingCustomer;

        var newCustomer = new Customer
        {
            Name = customerName,
            Document = customerDocument ?? "",
            Email = "",
            Phone = "",
            Address = ""
        };

        await _customerRepository.AddAsync(newCustomer);
        return newCustomer;
    }

    private async Task<ProductStock> GetOrCreateProductStockAsync(int productId, int storeId)
    {
        var productStock = await _productStockRepository.GetByProductAndStoreAsync(productId, storeId);
        if (productStock != null)
            return productStock;

        var newProductStock = new ProductStock
        {
            ProductId = productId,
            StoreId = storeId,
            CurrentStock = 0,
            MinimumStock = 0,
            MaximumStock = 1000
        };

        await _productStockRepository.AddAsync(newProductStock);
        return newProductStock;
    }

    private async Task<string> GenerateUniqueBatchCodeAsync(string prefix)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = new Random().Next(1000, 9999);
        return $"{prefix}_{timestamp}_{random}";
    }
}

public class SalesDocumentBatch
{
    public string DocumentNumber { get; set; } = string.Empty;
    public List<TandiaSaleDetailDto> SaleDetails { get; set; } = new();
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerDocument { get; set; }
    public DateTime SaleDate { get; set; }
}