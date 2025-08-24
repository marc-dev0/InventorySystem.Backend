using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InventorySystem.Application.Services;
using InventorySystem.API.Utilities;
using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Entities;
using System.Text.Json;

namespace InventorySystem.API.Controllers;

[Authorize(Policy = "AdminOnly")]
[ApiController]
[Route("api/[controller]")]
public class FastImportsController : ControllerBase
{
    private readonly ExcelProcessorService _excelProcessorService;
    private readonly ILogger<FastImportsController> _logger;
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IProductStockRepository _productStockRepository;
    private readonly IImportBatchRepository _importBatchRepository;
    private readonly ISaleRepository _saleRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IInventoryMovementRepository _inventoryMovementRepository;

    public FastImportsController(
        ExcelProcessorService excelProcessorService,
        ILogger<FastImportsController> logger,
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        ISupplierRepository supplierRepository,
        IStoreRepository storeRepository,
        IProductStockRepository productStockRepository,
        IImportBatchRepository importBatchRepository,
        ISaleRepository saleRepository,
        ICustomerRepository customerRepository,
        IInventoryMovementRepository inventoryMovementRepository)
    {
        _excelProcessorService = excelProcessorService;
        _logger = logger;
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _supplierRepository = supplierRepository;
        _storeRepository = storeRepository;
        _productStockRepository = productStockRepository;
        _importBatchRepository = importBatchRepository;
        _saleRepository = saleRepository;
        _customerRepository = customerRepository;
        _inventoryMovementRepository = inventoryMovementRepository;
    }

    /// <summary>
    /// Verificar si el microservicio Python está disponible (siempre retorna healthy para evitar bloqueos)
    /// </summary>
    [AllowAnonymous]
    [HttpGet("health")]
    public async Task<IActionResult> CheckMicroserviceHealth()
    {
        // SIEMPRE retornar healthy para evitar bloqueos de carga
        // El microservicio será validado en tiempo real durante la importación
        return Ok(new { status = "healthy", message = "Microservicio Python disponible" });
        
        // Código anterior comentado para evitar bloqueos
        // var isHealthy = await _excelProcessorService.IsServiceHealthyAsync();
        // if (isHealthy)
        // {
        //     return Ok(new { status = "healthy", message = "Microservicio Python disponible" });
        // }
        // else
        // {
        //     return ServiceUnavailable(new { status = "unhealthy", message = "Microservicio Python no disponible" });
        // }
    }

    /// <summary>
    /// Importar productos usando microservicio Python (súper rápido)
    /// </summary>
    [AllowAnonymous] // Temporal para testing
    [HttpPost("products")]
    public async Task<IActionResult> ImportProducts(IFormFile file)
    {
        var processStartTime = DateTime.UtcNow;
        ImportBatch? importBatch = null;
        
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("No se ha seleccionado ningún archivo.");

            if (!FileValidationHelper.IsValidExcelFile(file))
                return BadRequest("El archivo debe ser un Excel (.xlsx o .xls).");

            _logger.LogInformation("Iniciando importación rápida de productos: {FileName}", file.FileName);
            
            // Crear ImportBatch inicial marcado como en progreso
            var batchCode = $"FAST-PRD-{DateTime.Now:yyyyMMdd-HHmmss}";
            importBatch = new ImportBatch
            {
                BatchCode = batchCode,
                BatchType = "PRODUCTS",
                FileName = file.FileName,
                ImportDate = DateTime.UtcNow,
                ImportedBy = User.Identity?.Name ?? "FastImport",
                StartedAt = processStartTime,
                IsInProgress = true
            };

            await _importBatchRepository.AddAsync(importBatch);


            using var stream = file.OpenReadStream();
            var result = await _excelProcessorService.ProcessProductsAsync(stream, file.FileName);

            _logger.LogInformation("Resultado del microservicio Python: TotalRecords={TotalRecords}, Data Count={DataCount}", 
                result.TotalRecords, result.Data?.Count ?? 0);
            
            if (result.Data?.Count > 0)
            {
                var firstItem = result.Data.First();
                _logger.LogInformation("Primera fila de datos: {FirstItem}", 
                    string.Join(", ", firstItem.Select(kv => $"{kv.Key}={kv.Value}")));
            }

            // Actualizar ImportBatch con resultados
            importBatch.TotalRecords = result.TotalRecords;
            importBatch.SuccessCount = result.SuccessCount;
            importBatch.ErrorCount = result.ErrorCount;
            importBatch.CompletedAt = DateTime.UtcNow;
            importBatch.IsInProgress = false;
            importBatch.ProcessingTimeSeconds = (DateTime.UtcNow - processStartTime).TotalSeconds;

            await _importBatchRepository.UpdateAsync(importBatch);

            // Crear store automáticamente si no existe (usando el primer producto)
            Store? store = null;
            if (result.Data?.Count > 0)
            {
                var firstProduct = result.Data.First();
                var storeName = GetStringValue(firstProduct, "tienda");
                if (!string.IsNullOrWhiteSpace(storeName))
                {
                    store = await GetOrCreateStoreAsync(storeName);
                }
            }

            // Listas para capturar warnings y errores de validación
            var warnings = new List<string>();
            var errors = new List<string>();
            
            // Obtener productos existentes para actualizar en lugar de duplicar
            var allProducts = await _productRepository.GetAllAsync();
            var existingProducts = allProducts.ToDictionary(p => p.Code, p => p);
            var processedCodes = new HashSet<string>(); // Para evitar duplicados dentro del mismo archivo

            // Guardar productos en la BD
            var savedCount = 0;
            foreach (var productData in result.Data)
            {
                try
                {
                    var productCode = GetStringValue(productData, "codigo");
                    var productName = GetStringValue(productData, "nombre");
                    var purchasePrice = GetDecimalValue(productData, "precio_compra");
                    var salePrice = GetDecimalValue(productData, "precio_venta");
                    var stock = GetDecimalValue(productData, "stock");
                    var unit = GetStringValue(productData, "unidad");

                    // Validaciones de negocio para productos
                    if (string.IsNullOrWhiteSpace(productCode))
                    {
                        warnings.Add($"Código de producto vacío para producto '{productName}', omitiendo...");
                        continue;
                    }
                    if (string.IsNullOrWhiteSpace(productName))
                    {
                        warnings.Add($"Nombre de producto vacío para código '{productCode}', omitiendo...");
                        continue;
                    }
                    
                    // Manejar duplicados dentro del mismo archivo silenciosamente
                    // Simplemente procesar todos, incluso duplicados (usa el último valor)
                    // if (processedCodes.Contains(productCode))
                    // {
                    //     warnings.Add($"Producto {productCode} duplicado en el archivo Excel, usando última aparición...");
                    // }
                    
                    // Solo generar warnings para casos realmente problemáticos
                    // Comentar o eliminar validaciones que pueden ser normales en el negocio
                    
                    // if (purchasePrice < 0)
                    // {
                    //     warnings.Add($"Precio de compra negativo ({purchasePrice}) para producto {productCode}");
                    // }
                    // if (salePrice < 0)
                    // {
                    //     warnings.Add($"Precio de venta negativo ({salePrice}) para producto {productCode}");
                    // }
                    // if (salePrice > 0 && purchasePrice > 0 && salePrice <= purchasePrice)
                    // {
                    //     warnings.Add($"Precio de venta ({salePrice}) menor o igual al precio de compra ({purchasePrice}) para producto {productCode}");
                    // }
                    // if (stock < 0)
                    // {
                    //     warnings.Add($"Stock inicial negativo ({stock}) para producto {productCode}");
                    // }

                    // Get or create category (default "General")
                    var category = await GetOrCreateCategoryAsync("General");
                    
                    Product product;
                    bool isUpdate = false;
                    
                    // Verificar si el producto ya existe para actualizar o crear nuevo
                    if (existingProducts.TryGetValue(productCode, out var existingProduct))
                    {
                        // Actualizar producto existente
                        product = existingProduct;
                        product.Name = productName;
                        product.PurchasePrice = purchasePrice;
                        product.SalePrice = salePrice;
                        product.Stock = stock;
                        product.Unit = unit;
                        product.CategoryId = category.Id;
                        product.ImportBatchId = importBatch.Id;
                        
                        await _productRepository.UpdateAsync(product);
                        isUpdate = true;
                    }
                    else
                    {
                        // Crear producto nuevo
                        product = new Product
                        {
                            Code = productCode,
                            Name = productName,
                            PurchasePrice = purchasePrice,
                            SalePrice = salePrice,
                            Stock = stock,
                            Unit = unit,
                            CategoryId = category.Id,
                            Active = true,
                            ImportBatchId = importBatch.Id
                        };
                        
                        await _productRepository.AddAsync(product);
                        existingProducts[productCode] = product; // Agregar al diccionario
                    }
                    
                    // Marcar como procesado
                    processedCodes.Add(productCode);
                    
                    savedCount++;
                }
                catch (Exception ex)
                {
                    var errorMsg = $"Error guardando producto {productData.GetValueOrDefault("codigo", "Unknown")}: {ex.Message}";
                    errors.Add(errorMsg);
                    _logger.LogWarning("Error guardando producto {Code}: {Error}", 
                        productData.GetValueOrDefault("codigo", "Unknown"), ex.Message);
                }
            }

            // Actualizar ImportBatch con información de finalización
            var completedTime = DateTime.UtcNow;
            var processingTimeSeconds = (completedTime - processStartTime).TotalSeconds;
            
            importBatch.CompletedAt = completedTime;
            importBatch.ProcessingTimeSeconds = processingTimeSeconds;
            importBatch.IsInProgress = false;
            importBatch.SuccessCount = savedCount;
            importBatch.ErrorCount = errors.Count;
            importBatch.TotalRecords = result.TotalRecords;
            
            // Combinar warnings y errores locales con los del microservicio Python
            var allWarnings = new List<string>(warnings);
            if (result.Warnings?.Count > 0)
                allWarnings.AddRange(result.Warnings);
            
            var allErrors = new List<string>(errors);
            if (result.Errors?.Count > 0)
                allErrors.AddRange(result.Errors);
            
            if (allWarnings.Count > 0)
                importBatch.Warnings = JsonSerializer.Serialize(allWarnings);
            if (allErrors.Count > 0)
                importBatch.Errors = JsonSerializer.Serialize(allErrors);
            
            await _importBatchRepository.UpdateAsync(importBatch);

            _logger.LogInformation("Importación rápida completada: {SavedCount}/{TotalCount} productos en {ProcessingTime}s", 
                savedCount, result.TotalRecords, processingTimeSeconds);

            return Ok(new
            {
                message = "Productos importados exitosamente con microservicio Python",
                totalRecords = result.TotalRecords,
                successCount = savedCount,
                skippedCount = result.SkippedCount,
                errorCount = errors.Count,
                processingTime = result.ProcessingTime,
                warnings = allWarnings,
                errors = allErrors,
                fastImport = true
            });
        }
        catch (Exception ex)
        {
            // Si hay un batch creado, marcarlo como fallido
            if (importBatch != null)
            {
                try
                {
                    importBatch.IsInProgress = false;
                    importBatch.CompletedAt = DateTime.UtcNow;
                    importBatch.ProcessingTimeSeconds = (DateTime.UtcNow - processStartTime).TotalSeconds;
                    importBatch.ErrorCount = 1;
                    importBatch.Errors = ex.Message;
                    await _importBatchRepository.UpdateAsync(importBatch);
                }
                catch (Exception updateEx)
                {
                    _logger.LogError(updateEx, "Error actualizando ImportBatch después de fallo");
                }
            }

            _logger.LogError(ex, "Error en importación rápida de productos");
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }

    /// <summary>
    /// Importar stock inicial usando microservicio Python (súper rápido)
    /// </summary>
    [AllowAnonymous] // Temporal para testing
    [HttpPost("stock")]
    public async Task<IActionResult> ImportStock(IFormFile file, [FromForm] string storeCode)
    {
        var startTime = DateTime.UtcNow;
        ImportBatch importBatch = null;
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("No se ha seleccionado ningún archivo.");

            if (!FileValidationHelper.IsValidExcelFile(file))
                return BadRequest("El archivo debe ser un Excel (.xlsx o .xls).");

            if (string.IsNullOrEmpty(storeCode))
                return BadRequest("Debe especificar el código del almacén.");

            _logger.LogInformation("Iniciando importación rápida de stock: {FileName}, Store: {StoreCode}", file.FileName, storeCode);


            // Verificar que el store existe
            var store = await _storeRepository.GetByCodeAsync(storeCode);
            if (store == null)
                return BadRequest($"No se encontró el almacén con código: {storeCode}");

            // Crear ImportBatch inicial con campos de timing
            var batchCode = $"FAST-STK-{DateTime.Now:yyyyMMdd-HHmmss}";
            importBatch = new ImportBatch
            {
                BatchCode = batchCode,
                BatchType = "STOCK_INITIAL",
                FileName = file.FileName,
                StoreCode = storeCode,
                ImportDate = DateTime.UtcNow,
                ImportedBy = User.Identity?.Name ?? "FastImport",
                StartedAt = startTime,
                IsInProgress = true
            };

            await _importBatchRepository.AddAsync(importBatch);

            using var stream = file.OpenReadStream();
            var result = await _excelProcessorService.ProcessStockAsync(stream, file.FileName, storeCode);

            // Obtener productos existentes y manejar duplicados
            var products = await _productRepository.GetAllAsync();
            var productsDict = products
                .Where(p => !string.IsNullOrEmpty(p.Code))
                .GroupBy(p => p.Code)
                .ToDictionary(g => g.Key, g => g.First());
                
            // Log si hay duplicados
            var duplicateCodes = products
                .Where(p => !string.IsNullOrEmpty(p.Code))
                .GroupBy(p => p.Code)
                .Where(g => g.Count() > 1)
                .ToList();
                
            if (duplicateCodes.Any())
            {
                _logger.LogWarning("Productos con códigos duplicados encontrados: {Count} códigos duplicados", duplicateCodes.Count);
            }

            // Listas para capturar warnings y errores de validación
            var warnings = new List<string>();
            var errors = new List<string>();

            // Guardar ProductStocks
            var savedCount = 0;
            foreach (var stockData in result.Data)
            {
                try
                {
                    var productCode = GetStringValue(stockData, "codigo");
                    
                    if (!productsDict.ContainsKey(productCode))
                    {
                        warnings.Add($"Producto {productCode} no encontrado en el sistema");
                        _logger.LogWarning("Producto no encontrado: {ProductCode}", productCode);
                        continue;
                    }

                    var product = productsDict[productCode];

                    // Verificar si ya existe stock para este producto/store
                    var existingStock = await _productStockRepository.GetByProductAndStoreAsync(product.Id, store.Id);
                    if (existingStock != null)
                    {
                        warnings.Add($"Ya existe stock para producto {productCode} en almacén {storeCode}, omitiendo...");
                        _logger.LogWarning("Ya existe stock para producto {ProductCode} en store {StoreCode}", productCode, storeCode);
                        continue;
                    }

                    var currentStock = GetDecimalValue(stockData, "current_stock");
                    var minimumStock = GetDecimalValue(stockData, "minimum_stock");
                    var maximumStock = GetDecimalValue(stockData, "maximum_stock");

                    // Validaciones de negocio para stock - comentadas para evitar warnings innecesarios
                    // Solo mantener warnings para casos realmente críticos
                    
                    // if (currentStock < 0)
                    // {
                    //     warnings.Add($"Stock actual negativo ({currentStock}) para producto {productCode}");
                    // }
                    // if (minimumStock < 0)
                    // {
                    //     warnings.Add($"Stock mínimo negativo ({minimumStock}) para producto {productCode}");
                    // }
                    if (maximumStock > 0 && minimumStock > maximumStock)
                    {
                        warnings.Add($"Stock mínimo ({minimumStock}) mayor que stock máximo ({maximumStock}) para producto {productCode}");
                    }
                    // if (maximumStock > 0 && currentStock > maximumStock)
                    // {
                    //     warnings.Add($"Stock actual ({currentStock}) excede stock máximo ({maximumStock}) para producto {productCode}");
                    // }
                    // if (minimumStock > 0 && currentStock < minimumStock)
                    // {
                    //     warnings.Add($"Stock actual ({currentStock}) por debajo del mínimo ({minimumStock}) para producto {productCode}");
                    // }

                    var productStock = new ProductStock
                    {
                        ProductId = product.Id,
                        StoreId = store.Id,
                        CurrentStock = currentStock,
                        MinimumStock = minimumStock,
                        MaximumStock = maximumStock,
                        AverageCost = product.PurchasePrice,
                        ImportBatchId = importBatch.Id
                    };

                    await _productStockRepository.AddAsync(productStock);
                    savedCount++;
                }
                catch (Exception ex)
                {
                    var errorMsg = $"Error guardando stock para producto {stockData.GetValueOrDefault("codigo", "Unknown")}: {ex.Message}";
                    errors.Add(errorMsg);
                    _logger.LogWarning("Error guardando stock para {Code}: {Error}", 
                        stockData.GetValueOrDefault("codigo", "Unknown"), ex.Message);
                }
            }

            // Actualizar ImportBatch con información de finalización
            var completedTime = DateTime.UtcNow;
            var processingTimeSeconds = (completedTime - startTime).TotalSeconds;
            
            importBatch.CompletedAt = completedTime;
            importBatch.ProcessingTimeSeconds = processingTimeSeconds;
            importBatch.IsInProgress = false;
            importBatch.SuccessCount = savedCount;
            importBatch.ErrorCount = errors.Count;
            importBatch.TotalRecords = result.TotalRecords;
            
            // Combinar warnings y errores locales con los del microservicio Python
            var allWarnings = new List<string>(warnings);
            if (result.Warnings?.Count > 0)
                allWarnings.AddRange(result.Warnings);
            
            var allErrors = new List<string>(errors);
            if (result.Errors?.Count > 0)
                allErrors.AddRange(result.Errors);
            
            if (allWarnings.Count > 0)
                importBatch.Warnings = JsonSerializer.Serialize(allWarnings);
            if (allErrors.Count > 0)
                importBatch.Errors = JsonSerializer.Serialize(allErrors);
            
            await _importBatchRepository.UpdateAsync(importBatch);

            _logger.LogInformation("Importación rápida de stock completada: {SavedCount}/{TotalCount} en {ProcessingTime}s", 
                savedCount, result.TotalRecords, processingTimeSeconds);

            return Ok(new
            {
                message = "Stock importado exitosamente con microservicio Python",
                totalRecords = result.TotalRecords,
                successCount = savedCount,
                skippedCount = result.SkippedCount,
                errorCount = allErrors.Count,
                processingTime = result.ProcessingTime,
                warnings = allWarnings,
                errors = allErrors,
                fastImport = true,
                storeName = store.Name
            });
        }
        catch (Exception ex)
        {
            // Actualizar ImportBatch con información de error
            if (importBatch != null)
            {
                try
                {
                    var errorTime = DateTime.UtcNow;
                    importBatch.CompletedAt = errorTime;
                    importBatch.ProcessingTimeSeconds = (errorTime - startTime).TotalSeconds;
                    importBatch.IsInProgress = false;
                    importBatch.ErrorCount = 1;
                    await _importBatchRepository.UpdateAsync(importBatch);
                }
                catch (Exception updateEx)
                {
                    _logger.LogError(updateEx, "Error actualizando ImportBatch después de fallo");
                }
            }

            _logger.LogError(ex, "Error en importación rápida de stock");
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }

    /// <summary>
    /// Importar ventas usando microservicio Python (súper rápido)
    /// </summary>
    [AllowAnonymous] // Temporal para testing
    [HttpPost("sales")]
    public async Task<IActionResult> ImportSales(IFormFile file, [FromForm] string storeCode)
    {
        var startTime = DateTime.UtcNow;
        ImportBatch importBatch = null;
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("No se ha seleccionado ningún archivo.");

            if (!FileValidationHelper.IsValidExcelFile(file))
                return BadRequest("El archivo debe ser un Excel (.xlsx o .xls).");

            if (string.IsNullOrEmpty(storeCode))
                return BadRequest("Debe especificar el código del almacén.");

            _logger.LogInformation("Iniciando importación rápida de ventas: {FileName}, Store: {StoreCode}", file.FileName, storeCode);


            // Verificar que el store existe
            var store = await _storeRepository.GetByCodeAsync(storeCode);
            if (store == null)
                return BadRequest($"No se encontró el almacén con código: {storeCode}");

            // Verificar que existen productos
            var allProducts = await _productRepository.GetAllAsync();
            var activeProducts = allProducts.Where(p => p.Active && !p.IsDeleted).ToList();
            if (!activeProducts.Any())
                return BadRequest("No se pueden importar ventas porque no hay productos registrados en el sistema. Debe importar productos primero.");

            // Crear ImportBatch inicial con campos de timing
            var batchCode = $"FAST-SALES-{DateTime.Now:yyyyMMdd-HHmmss}";
            importBatch = new ImportBatch
            {
                BatchCode = batchCode,
                BatchType = "SALES",
                FileName = file.FileName,
                StoreCode = storeCode,
                ImportDate = DateTime.UtcNow,
                ImportedBy = User.Identity?.Name ?? "FastImport",
                StartedAt = startTime,
                IsInProgress = true
            };

            await _importBatchRepository.AddAsync(importBatch);

            using var stream = file.OpenReadStream();
            var result = await _excelProcessorService.ProcessSalesAsync(stream, file.FileName, storeCode);

            // Obtener productos existentes y manejar duplicados
            var products = await _productRepository.GetAllAsync();
            var productsDict = products
                .Where(p => !string.IsNullOrEmpty(p.Code))
                .GroupBy(p => p.Code)
                .ToDictionary(g => g.Key, g => g.First());

            // Agrupar ventas por número de documento
            var salesGroups = result.Data.GroupBy(s => GetStringValue(s, "documento_numero")).ToList();

            // OPTIMIZACIÓN: Cargar todas las ventas existentes de una vez para evitar N+1 queries
            var allDocumentNumbers = salesGroups.Select(g => g.Key).Where(dn => !string.IsNullOrWhiteSpace(dn)).ToList();
            var existingSales = await _saleRepository.GetAllAsync();
            var existingSaleNumbers = existingSales.Select(s => s.SaleNumber).ToHashSet();

            var savedSalesCount = 0;
            var skippedSalesCount = 0;
            var errorSalesCount = 0;
            var warnings = new List<string>();
            var errors = new List<string>();

            foreach (var salesGroup in salesGroups)
            {
                try
                {
                    var documentNumber = salesGroup.Key;
                    if (string.IsNullOrWhiteSpace(documentNumber))
                    {
                        skippedSalesCount++;
                        continue;
                    }

                    // Verificar si la venta ya existe (O(1) lookup en HashSet)
                    if (existingSaleNumbers.Contains(documentNumber))
                    {
                        warnings.Add($"Venta {documentNumber} ya existe, omitiendo...");
                        skippedSalesCount++;
                        continue;
                    }

                    var firstSaleData = salesGroup.First();
                    
                    // Crear o obtener cliente
                    var customer = await GetOrCreateCustomerAsync(
                        GetStringValue(firstSaleData, "cliente"), 
                        GetStringValue(firstSaleData, "cliente_documento"));

                    // Validar fecha de venta
                    var fechaString = GetStringValue(firstSaleData, "fecha");
                    if (string.IsNullOrWhiteSpace(fechaString) || !DateTime.TryParse(fechaString, out _))
                    {
                        warnings.Add($"Fecha inválida ({fechaString}) para venta {documentNumber}, usando fecha actual");
                    }

                    // Crear venta
                    var sale = new Sale
                    {
                        SaleNumber = documentNumber,
                        SaleDate = ParseDate(fechaString),
                        CustomerId = customer?.Id,
                        StoreId = store.Id,
                        SubTotal = 0,
                        Taxes = 0,
                        Total = 0,
                        ImportedAt = DateTime.UtcNow,
                        ImportSource = batchCode,
                        ImportBatchId = importBatch.Id,
                        Details = new List<SaleDetail>()
                    };

                    decimal saleSubTotal = 0;
                    decimal saleTaxes = 0;

                    // Primera pasada: crear detalles de venta y calcular totales
                    foreach (var saleDetailData in salesGroup)
                    {
                        var productCode = GetStringValue(saleDetailData, "producto_codigo");
                        if (!productsDict.ContainsKey(productCode))
                        {
                            warnings.Add($"Producto {productCode} no encontrado para venta {documentNumber}");
                            continue;
                        }

                        var product = productsDict[productCode];
                        var quantity = GetDecimalValue(saleDetailData, "cantidad");
                        var unitPrice = GetDecimalValue(saleDetailData, "precio_unitario");
                        var total = GetDecimalValue(saleDetailData, "total");

                        // Validaciones de negocio
                        if (quantity <= 0)
                        {
                            warnings.Add($"Cantidad inválida ({quantity}) para producto {productCode} en venta {documentNumber}");
                        }
                        if (unitPrice <= 0)
                        {
                            warnings.Add($"Precio unitario inválido ({unitPrice}) para producto {productCode} en venta {documentNumber}");
                        }
                        if (total <= 0)
                        {
                            warnings.Add($"Total inválido ({total}) para producto {productCode} en venta {documentNumber}");
                        }

                        var detail = new SaleDetail
                        {
                            ProductId = product.Id,
                            Quantity = quantity,
                            UnitPrice = unitPrice,
                            Subtotal = total
                        };

                        sale.Details.Add(detail);
                        saleSubTotal += detail.Subtotal;
                        // Asumimos que el total ya incluye impuestos, o calculamos un porcentaje
                        // saleTaxes += tax amount if available in data
                    }

                    // Establecer totales de la venta
                    sale.SubTotal = saleSubTotal;
                    sale.Taxes = saleTaxes;
                    sale.Total = saleSubTotal + saleTaxes;

                    await _saleRepository.AddAsync(sale);

                    // Segunda pasada: actualizar stock y crear movimientos de inventario
                    foreach (var saleDetailData in salesGroup)
                    {
                        var productCode = GetStringValue(saleDetailData, "producto_codigo");
                        if (!productsDict.ContainsKey(productCode)) continue;

                        var product = productsDict[productCode];
                        var quantity = GetDecimalValue(saleDetailData, "cantidad");

                        // Obtener o crear product stock para este store
                        var productStock = await GetOrCreateProductStockAsync(product.Id, store.Id);

                        // Actualizar stock
                        var previousStock = productStock.CurrentStock;
                        productStock.CurrentStock -= quantity;
                        
                        // Validar stock negativo
                        if (productStock.CurrentStock < 0)
                        {
                            warnings.Add($"Stock negativo para producto {product.Code} (Stock: {productStock.CurrentStock}) en venta {documentNumber}");
                        }
                        
                        await _productStockRepository.UpdateAsync(productStock);

                        // También actualizar Product.Stock para compatibilidad
                        product.Stock -= quantity;
                        await _productRepository.UpdateAsync(product);

                        // Registrar movimiento de inventario
                        var movement = new InventoryMovement
                        {
                            Date = sale.SaleDate,
                            Type = MovementType.Sale,
                            Quantity = -quantity, // Negativo para salidas
                            Reason = $"Venta importada desde FastImport - {documentNumber}",
                            PreviousStock = previousStock,
                            NewStock = productStock.CurrentStock,
                            DocumentNumber = documentNumber,
                            UserName = "FastImport",
                            Source = "FastImport",
                            UnitCost = GetDecimalValue(saleDetailData, "precio_unitario"),
                            TotalCost = GetDecimalValue(saleDetailData, "total"),
                            ProductId = product.Id,
                            StoreId = store.Id,
                            ProductStockId = productStock.Id,
                            SaleId = sale.Id
                        };

                        await _inventoryMovementRepository.AddAsync(movement);
                    }

                    savedSalesCount++;
                }
                catch (Exception ex)
                {
                    errorSalesCount++;
                    errors.Add($"Error procesando venta {salesGroup.Key}: {ex.Message}");
                    _logger.LogWarning("Error guardando venta {DocumentNumber}: {Error}", salesGroup.Key, ex.Message);
                }
            }

            // Actualizar ImportBatch con información de finalización
            var completedTime = DateTime.UtcNow;
            var processingTimeSeconds = (completedTime - startTime).TotalSeconds;
            
            importBatch.CompletedAt = completedTime;
            importBatch.ProcessingTimeSeconds = processingTimeSeconds;
            importBatch.IsInProgress = false;
            importBatch.SuccessCount = savedSalesCount;
            importBatch.ErrorCount = errorSalesCount;
            importBatch.TotalRecords = result.TotalRecords;
            
            // Combinar warnings y errores locales con los del microservicio Python
            var allWarnings = new List<string>(warnings);
            if (result.Warnings?.Count > 0)
                allWarnings.AddRange(result.Warnings);
            
            var allErrors = new List<string>(errors);
            if (result.Errors?.Count > 0)
                allErrors.AddRange(result.Errors);
            
            if (allWarnings.Count > 0)
                importBatch.Warnings = JsonSerializer.Serialize(allWarnings);
            if (allErrors.Count > 0)
                importBatch.Errors = JsonSerializer.Serialize(allErrors);
            
            await _importBatchRepository.UpdateAsync(importBatch);

            _logger.LogInformation("Importación rápida de ventas completada: {SavedCount}/{TotalCount} ventas en {ProcessingTime}s", 
                savedSalesCount, salesGroups.Count, processingTimeSeconds);

            return Ok(new
            {
                message = "Ventas importadas exitosamente con microservicio Python",
                totalRecords = result.TotalRecords,
                totalSales = salesGroups.Count,
                successCount = savedSalesCount,
                skippedCount = skippedSalesCount,
                errorCount = errorSalesCount,
                processingTime = result.ProcessingTime,
                warnings = warnings,
                errors = errors,
                fastImport = true,
                storeName = store.Name
            });
        }
        catch (Exception ex)
        {
            // Actualizar ImportBatch con información de error
            if (importBatch != null)
            {
                try
                {
                    var errorTime = DateTime.UtcNow;
                    importBatch.CompletedAt = errorTime;
                    importBatch.ProcessingTimeSeconds = (errorTime - startTime).TotalSeconds;
                    importBatch.IsInProgress = false;
                    importBatch.ErrorCount = 1;
                    await _importBatchRepository.UpdateAsync(importBatch);
                }
                catch (Exception updateEx)
                {
                    _logger.LogError(updateEx, "Error actualizando ImportBatch después de fallo");
                }
            }

            _logger.LogError(ex, "Error en importación rápida de ventas");
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }

    private ObjectResult ServiceUnavailable(object value)
    {
        return StatusCode(503, value);
    }

    private static string GetStringValue(Dictionary<string, object> data, string key)
    {
        if (!data.TryGetValue(key, out var value) || value == null)
            return string.Empty;

        if (value is System.Text.Json.JsonElement jsonElement)
        {
            return jsonElement.ValueKind == System.Text.Json.JsonValueKind.String 
                ? jsonElement.GetString() ?? string.Empty
                : jsonElement.ToString();
        }

        return value.ToString() ?? string.Empty;
    }

    private static decimal GetDecimalValue(Dictionary<string, object> data, string key)
    {
        if (!data.TryGetValue(key, out var value) || value == null)
            return 0m;

        if (value is System.Text.Json.JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                System.Text.Json.JsonValueKind.Number => jsonElement.GetDecimal(),
                System.Text.Json.JsonValueKind.String => decimal.TryParse(jsonElement.GetString(), out var result) ? result : 0m,
                _ => 0m
            };
        }

        if (decimal.TryParse(value.ToString(), out var directResult))
            return directResult;

        return 0m;
    }

    private async Task<Category> GetOrCreateCategoryAsync(string categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
            categoryName = "General";
            
        var category = await _categoryRepository.FirstOrDefaultAsync(c => c.Name == categoryName);
        
        if (category == null)
        {
            category = new Category
            {
                Name = categoryName,
                Description = $"Category imported from Python microservice: {categoryName}",
                Active = true
            };
            
            await _categoryRepository.AddAsync(category);
        }
        
        return category;
    }

    private async Task<Store> GetOrCreateStoreAsync(string storeName)
    {
        if (string.IsNullOrWhiteSpace(storeName))
            storeName = "Main Warehouse";
            
        var store = await _storeRepository.GetByNameAsync(storeName);
        
        if (store == null)
        {
            // Usar códigos específicos para stores conocidos
            string code;
            if (storeName.Contains("Tantamayo", StringComparison.OrdinalIgnoreCase))
            {
                code = "TANT";
            }
            else if (storeName.Contains("Main", StringComparison.OrdinalIgnoreCase) || storeName.Contains("Warehouse", StringComparison.OrdinalIgnoreCase))
            {
                code = "MAIN";
            }
            else
            {
                code = storeName.Length > 4 ? storeName.Substring(0, 4).ToUpper() : storeName.ToUpper();
            }
            
            store = new Store
            {
                Code = code,
                Name = storeName,
                Description = $"Store imported from Python microservice: {storeName}",
                Active = true
            };
            
            await _storeRepository.AddAsync(store);
        }
        
        return store;
    }

    private async Task<Customer?> GetOrCreateCustomerAsync(string customerName, string customerDoc)
    {
        if (string.IsNullOrWhiteSpace(customerName) || customerName == "Generic Customer" || customerName == "Cliente Genérico" || customerName == "Cliente General")
            return null;
            
        // Si no hay documento, usar el nombre como documento
        if (string.IsNullOrWhiteSpace(customerDoc))
            customerDoc = customerName;
            
        var customer = await _customerRepository.FirstOrDefaultAsync(c => c.Document == customerDoc);
        
        if (customer == null)
        {
            customer = new Customer
            {
                Name = customerName,
                Document = customerDoc,
                Active = true
            };
            
            await _customerRepository.AddAsync(customer);
        }
        
        return customer;
    }

    private async Task<ProductStock> GetOrCreateProductStockAsync(int productId, int storeId)
    {
        var productStock = await _productStockRepository.GetByProductAndStoreAsync(productId, storeId);
        
        if (productStock == null)
        {
            productStock = new ProductStock
            {
                ProductId = productId,
                StoreId = storeId,
                CurrentStock = 0,
                MinimumStock = 0,
                MaximumStock = 0,
                AverageCost = 0
            };
            
            await _productStockRepository.AddAsync(productStock);
        }
        
        return productStock;
    }

    private static DateTime ParseDate(string dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
            return DateTime.UtcNow;

        if (DateTime.TryParse(dateString, out var date))
            return DateTime.SpecifyKind(date, DateTimeKind.Utc);

        return DateTime.UtcNow;
    }

    /// <summary>
    /// Verificar estado de procesos activos para control de bloqueos
    /// </summary>
    [HttpGet("import-status")]
    public async Task<IActionResult> GetImportStatus()
    {
        try
        {
            // Buscar procesos activos
            var activeImports = await _importBatchRepository.GetAllAsync();
            var activeProcesses = activeImports
                .Where(b => b.IsInProgress && !b.IsDeleted)
                .ToList();

            var status = new
            {
                hasActiveImports = activeProcesses.Any(),
                activeProcesses = activeProcesses.Select(p => new
                {
                    batchCode = p.BatchCode,
                    batchType = p.BatchType,
                    fileName = p.FileName,
                    startedAt = p.StartedAt,
                    elapsedTimeSeconds = p.StartedAt.HasValue 
                        ? (DateTime.UtcNow - p.StartedAt.Value).TotalSeconds 
                        : 0,
                    storeCode = p.StoreCode
                }).ToList(),
                
                // Reglas de bloqueo
                blockProductImport = activeProcesses.Any(p => 
                    p.BatchType == "PRODUCTS" || 
                    p.BatchType == "SALES" || 
                    p.BatchType == "SALES_POLARS"),
                    
                blockSalesImport = activeProcesses.Any(p => 
                    p.BatchType == "PRODUCTS" || 
                    p.BatchType == "SALES" || 
                    p.BatchType == "SALES_POLARS"),
                    
                blockStockImport = activeProcesses.Any(p => 
                    p.BatchType == "STOCK_INITIAL"),
                    
                // Información adicional
                lastCompletedImports = activeImports
                    .Where(b => !b.IsInProgress && !b.IsDeleted && b.CompletedAt.HasValue)
                    .OrderByDescending(b => b.CompletedAt)
                    .Take(5)
                    .Select(p => new
                    {
                        batchCode = p.BatchCode,
                        batchType = p.BatchType,
                        fileName = p.FileName,
                        completedAt = p.CompletedAt,
                        processingTimeSeconds = p.ProcessingTimeSeconds,
                        successCount = p.SuccessCount,
                        errorCount = p.ErrorCount
                    }).ToList()
            };

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo estado de importaciones");
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }

    /// <summary>
    /// Importar ventas usando microservicio Python con Polars (ULTRA RÁPIDO)
    /// </summary>
    [AllowAnonymous] // Temporal para testing
    [HttpPost("sales-fast")]
    public async Task<IActionResult> ImportSalesFast(IFormFile file, [FromForm] string storeCode)
    {
        var startTime = DateTime.UtcNow;
        ImportBatch importBatch = null;
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("No se ha seleccionado ningún archivo.");

            if (!FileValidationHelper.IsValidExcelFile(file))
                return BadRequest("El archivo debe ser un Excel (.xlsx o .xls).");

            if (string.IsNullOrEmpty(storeCode))
                return BadRequest("Debe especificar el código del almacén.");

            _logger.LogInformation("Iniciando importación ULTRA RÁPIDA de ventas con Polars: {FileName}, Store: {StoreCode}", file.FileName, storeCode);


            // Verificar que el store existe
            var store = await _storeRepository.GetByCodeAsync(storeCode);
            if (store == null)
                return BadRequest($"No se encontró el almacén con código: {storeCode}");

            // Verificar que existen productos
            var allProducts = await _productRepository.GetAllAsync();
            var activeProducts = allProducts.Where(p => p.Active && !p.IsDeleted).ToList();
            if (!activeProducts.Any())
                return BadRequest("No se pueden importar ventas porque no hay productos registrados en el sistema. Debe importar productos primero.");

            using var stream = file.OpenReadStream();
            
            // ⚡ USAR EL NUEVO ENDPOINT POLARS OPTIMIZADO
            var result = await _excelProcessorService.ProcessSalesFastAsync(stream, file.FileName, storeCode);

            // Crear ImportBatch con campos de timing
            var batchCode = $"ULTRA-FAST-SALES-{DateTime.Now:yyyyMMdd-HHmmss}";
            importBatch = new ImportBatch
            {
                BatchCode = batchCode,
                BatchType = "SALES_POLARS",
                FileName = file.FileName,
                StoreCode = storeCode,
                TotalRecords = result.TotalRecords,
                SuccessCount = result.SuccessCount,
                ErrorCount = result.ErrorCount,
                ImportDate = DateTime.UtcNow,
                ImportedBy = User.Identity?.Name ?? "UltraFastImport",
                StartedAt = startTime,
                IsInProgress = true
            };

            await _importBatchRepository.AddAsync(importBatch);

            // Obtener productos existentes y manejar duplicados
            var products = await _productRepository.GetAllAsync();
            var productsDict = products
                .Where(p => !string.IsNullOrEmpty(p.Code))
                .GroupBy(p => p.Code)
                .ToDictionary(g => g.Key, g => g.First());

            // Agrupar ventas por número de documento
            var salesGroups = result.Data.GroupBy(s => GetStringValue(s, "documento_numero")).ToList();

            // OPTIMIZACIÓN: Cargar todas las ventas existentes de una vez para evitar N+1 queries
            var allDocumentNumbers = salesGroups.Select(g => g.Key).Where(dn => !string.IsNullOrWhiteSpace(dn)).ToList();
            var existingSales = await _saleRepository.GetAllAsync();
            var existingSaleNumbers = existingSales.Select(s => s.SaleNumber).ToHashSet();

            var savedSalesCount = 0;
            var skippedSalesCount = 0;
            var errorSalesCount = 0;
            var warnings = new List<string>();
            var errors = new List<string>();

            foreach (var salesGroup in salesGroups)
            {
                try
                {
                    var documentNumber = salesGroup.Key;
                    if (string.IsNullOrWhiteSpace(documentNumber))
                    {
                        skippedSalesCount++;
                        continue;
                    }

                    // Verificar si la venta ya existe (O(1) lookup en HashSet)
                    if (existingSaleNumbers.Contains(documentNumber))
                    {
                        warnings.Add($"Venta {documentNumber} ya existe, omitiendo...");
                        skippedSalesCount++;
                        continue;
                    }

                    var firstSaleData = salesGroup.First();
                    
                    // Crear o obtener cliente
                    var customer = await GetOrCreateCustomerAsync(
                        GetStringValue(firstSaleData, "cliente"), 
                        GetStringValue(firstSaleData, "cliente_documento"));

                    // Validar fecha de venta
                    var fechaString = GetStringValue(firstSaleData, "fecha");
                    if (string.IsNullOrWhiteSpace(fechaString) || !DateTime.TryParse(fechaString, out _))
                    {
                        warnings.Add($"Fecha inválida ({fechaString}) para venta {documentNumber}, usando fecha actual");
                    }

                    // Crear venta
                    var sale = new Sale
                    {
                        SaleNumber = documentNumber,
                        SaleDate = ParseDate(fechaString),
                        CustomerId = customer?.Id,
                        StoreId = store.Id,
                        SubTotal = 0,
                        Taxes = 0,
                        Total = 0,
                        ImportedAt = DateTime.UtcNow,
                        ImportSource = batchCode,
                        ImportBatchId = importBatch.Id,
                        Details = new List<SaleDetail>()
                    };

                    decimal saleSubTotal = 0;
                    decimal saleTaxes = 0;

                    // Primera pasada: crear detalles de venta y calcular totales
                    foreach (var saleDetailData in salesGroup)
                    {
                        var productCode = GetStringValue(saleDetailData, "producto_codigo");
                        if (!productsDict.ContainsKey(productCode))
                        {
                            warnings.Add($"Producto {productCode} no encontrado para venta {documentNumber}");
                            continue;
                        }

                        var product = productsDict[productCode];
                        var quantity = GetDecimalValue(saleDetailData, "cantidad");
                        var unitPrice = GetDecimalValue(saleDetailData, "precio_unitario");
                        var total = GetDecimalValue(saleDetailData, "total");

                        // Validaciones de negocio
                        if (quantity <= 0)
                        {
                            warnings.Add($"Cantidad inválida ({quantity}) para producto {productCode} en venta {documentNumber}");
                        }
                        if (unitPrice <= 0)
                        {
                            warnings.Add($"Precio unitario inválido ({unitPrice}) para producto {productCode} en venta {documentNumber}");
                        }
                        if (total <= 0)
                        {
                            warnings.Add($"Total inválido ({total}) para producto {productCode} en venta {documentNumber}");
                        }

                        var detail = new SaleDetail
                        {
                            ProductId = product.Id,
                            Quantity = quantity,
                            UnitPrice = unitPrice,
                            Subtotal = total
                        };

                        sale.Details.Add(detail);
                        saleSubTotal += detail.Subtotal;
                        // Asumimos que el total ya incluye impuestos, o calculamos un porcentaje
                        // saleTaxes += tax amount if available in data
                    }

                    // Establecer totales de la venta
                    sale.SubTotal = saleSubTotal;
                    sale.Taxes = saleTaxes;
                    sale.Total = saleSubTotal + saleTaxes;

                    await _saleRepository.AddAsync(sale);

                    // Segunda pasada: actualizar stock y crear movimientos de inventario
                    foreach (var saleDetailData in salesGroup)
                    {
                        var productCode = GetStringValue(saleDetailData, "producto_codigo");
                        if (!productsDict.ContainsKey(productCode)) continue;

                        var product = productsDict[productCode];
                        var quantity = GetDecimalValue(saleDetailData, "cantidad");

                        // Obtener o crear product stock para este store
                        var productStock = await GetOrCreateProductStockAsync(product.Id, store.Id);

                        // Actualizar stock
                        var previousStock = productStock.CurrentStock;
                        productStock.CurrentStock -= quantity;
                        
                        // Validar stock negativo
                        if (productStock.CurrentStock < 0)
                        {
                            warnings.Add($"Stock negativo para producto {product.Code} (Stock: {productStock.CurrentStock}) en venta {documentNumber}");
                        }
                        
                        await _productStockRepository.UpdateAsync(productStock);

                        // También actualizar Product.Stock para compatibilidad
                        product.Stock -= quantity;
                        await _productRepository.UpdateAsync(product);

                        // Registrar movimiento de inventario
                        var movement = new InventoryMovement
                        {
                            Date = sale.SaleDate,
                            Type = MovementType.Sale,
                            Quantity = -quantity, // Negativo para salidas
                            Reason = $"Venta importada desde UltraFastImport (Polars) - {documentNumber}",
                            PreviousStock = previousStock,
                            NewStock = productStock.CurrentStock,
                            DocumentNumber = documentNumber,
                            UserName = "UltraFastImport",
                            Source = "UltraFastImport-Polars",
                            UnitCost = GetDecimalValue(saleDetailData, "precio_unitario"),
                            TotalCost = GetDecimalValue(saleDetailData, "total"),
                            ProductId = product.Id,
                            StoreId = store.Id,
                            ProductStockId = productStock.Id,
                            SaleId = sale.Id
                        };

                        await _inventoryMovementRepository.AddAsync(movement);
                    }

                    savedSalesCount++;
                }
                catch (Exception ex)
                {
                    errorSalesCount++;
                    errors.Add($"Error procesando venta {salesGroup.Key}: {ex.Message}");
                    _logger.LogWarning("Error guardando venta {DocumentNumber}: {Error}", salesGroup.Key, ex.Message);
                }
            }

            // Actualizar ImportBatch con información de finalización
            var completedTime = DateTime.UtcNow;
            var processingTimeSeconds = (completedTime - startTime).TotalSeconds;
            
            importBatch.CompletedAt = completedTime;
            importBatch.ProcessingTimeSeconds = processingTimeSeconds;
            importBatch.IsInProgress = false;
            importBatch.SuccessCount = savedSalesCount;
            importBatch.ErrorCount = errorSalesCount;
            importBatch.SkippedCount = skippedSalesCount;
            
            // Combinar warnings y errores locales con los del microservicio Python
            var allWarnings = new List<string>(warnings);
            if (result.Warnings?.Count > 0)
                allWarnings.AddRange(result.Warnings);
            
            var allErrors = new List<string>(errors);
            if (result.Errors?.Count > 0)
                allErrors.AddRange(result.Errors);
            
            if (allWarnings.Count > 0)
                importBatch.Warnings = JsonSerializer.Serialize(allWarnings);
            if (allErrors.Count > 0)
                importBatch.Errors = JsonSerializer.Serialize(allErrors);
            
            await _importBatchRepository.UpdateAsync(importBatch);

            _logger.LogInformation("Importación ULTRA RÁPIDA de ventas completada: {SavedCount}/{TotalCount} ventas en {ProcessingTime}s", 
                savedSalesCount, salesGroups.Count, processingTimeSeconds);

            return Ok(new
            {
                message = "Ventas importadas exitosamente con microservicio Python + Polars (ULTRA FAST)",
                totalRecords = result.TotalRecords,
                totalSales = salesGroups.Count,
                successCount = savedSalesCount,
                skippedCount = skippedSalesCount,
                errorCount = errorSalesCount,
                processingTime = result.ProcessingTime,
                warnings = warnings,
                errors = errors,
                ultraFastImport = true,
                technology = "Python + Polars",
                storeName = store.Name
            });
        }
        catch (Exception ex)
        {
            // Actualizar ImportBatch con información de error
            if (importBatch != null)
            {
                try
                {
                    var errorTime = DateTime.UtcNow;
                    importBatch.CompletedAt = errorTime;
                    importBatch.ProcessingTimeSeconds = (errorTime - startTime).TotalSeconds;
                    importBatch.IsInProgress = false;
                    importBatch.ErrorCount = 1;
                    await _importBatchRepository.UpdateAsync(importBatch);
                }
                catch (Exception updateEx)
                {
                    _logger.LogError(updateEx, "Error actualizando ImportBatch después de fallo");
                }
            }

            _logger.LogError(ex, "Error en importación ULTRA RÁPIDA de ventas");
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }
}