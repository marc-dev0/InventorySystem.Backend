using ClosedXML.Excel;
using InventorySystem.Application.Interfaces;
using InventorySystem.Application.DTOs;
using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Entities;
using Microsoft.Extensions.Logging;

namespace InventorySystem.Application.Services;

public class StockInitialService : IStockInitialService
{
    private readonly IProductRepository _productRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IProductStockRepository _productStockRepository;
    private readonly IInventoryMovementRepository _inventoryMovementRepository;
    private readonly IImportBatchRepository _importBatchRepository;
    private readonly ISystemConfigurationRepository _configRepository;
    private readonly ILogger<StockInitialService> _logger;

    public StockInitialService(
        IProductRepository productRepository,
        IStoreRepository storeRepository,
        IProductStockRepository productStockRepository,
        IInventoryMovementRepository inventoryMovementRepository,
        IImportBatchRepository importBatchRepository,
        ISystemConfigurationRepository configRepository,
        ILogger<StockInitialService> logger)
    {
        _productRepository = productRepository;
        _storeRepository = storeRepository;
        _productStockRepository = productStockRepository;
        _inventoryMovementRepository = inventoryMovementRepository;
        _importBatchRepository = importBatchRepository;
        _configRepository = configRepository;
        _logger = logger;
    }

    public async Task<StockLoadResultDto> LoadStockFromExcelAsync(Stream excelStream, string fileName, string storeCode)
    {
        var startTime = DateTime.UtcNow;
        var result = new StockLoadResultDto { StoreCode = storeCode };
        
        _logger.LogInformation("Iniciando carga de stock para archivo: {FileName}, Store: {StoreCode}", fileName, storeCode);

        // Validar store
        var store = await _storeRepository.GetByCodeAsync(storeCode);
        if (store == null)
        {
            result.Errors.Add($"No se encontró el almacén con código: {storeCode}");
            return result;
        }

        result.StoreName = store.Name;

        // Crear y guardar ImportBatch primero para obtener el Id
        var batchCode = $"STK-{DateTime.Now:yyyyMMdd-HHmmss}";
        var importBatch = new ImportBatch
        {
            BatchCode = batchCode,
            BatchType = "STOCK_INITIAL",
            FileName = fileName,
            StoreCode = storeCode,
            ImportDate = DateTime.UtcNow,
            ImportedBy = "System" // TODO: Obtener usuario actual
        };

        // Guardar el batch primero para obtener el ID
        await _importBatchRepository.AddAsync(importBatch);

        // Obtener todos los productos existentes
        var products = await _productRepository.GetAllAsync();
        // Manejar productos duplicados tomando el primero de cada código
        var productsDict = products
            .Where(p => !string.IsNullOrEmpty(p.Code))
            .GroupBy(p => p.Code)
            .ToDictionary(g => g.Key, g => g.First());
            
        // Log si hay duplicados
        var duplicateCodes = products
            .Where(p => !string.IsNullOrEmpty(p.Code))
            .GroupBy(p => p.Code)
            .Where(g => g.Count() > 1)
            .Select(g => new { Code = g.Key, Count = g.Count() })
            .ToList();
            
        if (duplicateCodes.Any())
        {
            _logger.LogWarning("Productos con códigos duplicados encontrados: {DuplicateCodes}", 
                string.Join(", ", duplicateCodes.Select(d => $"{d.Code}({d.Count})")));
        }
        
        _logger.LogInformation("Productos en BD: {ProductCount}, Store encontrado: {StoreName}", products.Count(), store.Name);

        try
        {
            using var workbook = new XLWorkbook(excelStream);
            var worksheet = workbook.Worksheet(1);

            // Verificar que el worksheet tiene datos
            if (worksheet.LastRowUsed() == null)
            {
                result.Errors.Add("El archivo Excel está vacío");
                return result;
            }

            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
            
            _logger.LogInformation("Archivo Excel abierto, última fila: {LastRow}", lastRow);

            // Obtener configuración de mapeo de columnas
            var columnMapping = await GetColumnMappingAsync();
            _logger.LogInformation("Usando mapeo de columnas: Código=Col{CodeColumn}, Stock=Col{StockColumn}, StockMin=Col{MinStockColumn}, InicioFila={StartRow}", 
                columnMapping.CodeColumn, columnMapping.StockColumn, columnMapping.MinStockColumn, columnMapping.StartRow);
            
            // Procesar desde la fila configurada (por defecto fila 2)
            for (int row = columnMapping.StartRow; row <= lastRow; row++)
            {
                try
                {
                    var codigo = worksheet.Cell(row, columnMapping.CodeColumn).GetString().Trim(); // Código
                    var stockStr = worksheet.Cell(row, columnMapping.StockColumn).GetString().Trim(); // Stock
                    var stockMinStr = worksheet.Cell(row, columnMapping.MinStockColumn).GetString().Trim(); // Stock min

                    // Log solo productos válidos procesados
                    if (!string.IsNullOrEmpty(codigo) && decimal.TryParse(stockStr, out _)) {
                        _logger.LogDebug("Procesando fila {Row}: Código='{Codigo}', Stock='{Stock}'", row, codigo, stockStr);
                    }

                    if (string.IsNullOrEmpty(codigo))
                    {
                        result.SkippedProducts++;
                        continue;
                    }

                    // Buscar producto por código
                    if (!productsDict.ContainsKey(codigo))
                    {
                        result.Warnings.Add($"Fila {row}: Producto con código {codigo} no encontrado en la BD");
                        result.SkippedProducts++;
                        continue;
                    }

                    var product = productsDict[codigo];

                    // Parsear stock
                    decimal currentStock = 0;
                    decimal minStock = 0;

                    if (!string.IsNullOrEmpty(stockStr))
                    {
                        if (!decimal.TryParse(stockStr, out currentStock))
                        {
                            result.Warnings.Add($"Fila {row}: Stock inválido para producto {codigo}: {stockStr}");
                        }
                    }

                    if (!string.IsNullOrEmpty(stockMinStr))
                    {
                        if (!decimal.TryParse(stockMinStr, out minStock))
                        {
                            result.Warnings.Add($"Fila {row}: Stock mínimo inválido para producto {codigo}: {stockMinStr}");
                        }
                    }

                    // Verificar si ya existe ProductStock para este producto y store
                    var existingStock = await _productStockRepository.GetByProductAndStoreAsync(product.Id, store.Id);
                    
                    if (existingStock != null)
                    {
                        // Skip existing stock - no update (incremental load)
                        result.SkippedProducts++;
                        continue;
                    }
                    
                    // Crear nuevo ProductStock asociado al ImportBatch
                    var productStock = new ProductStock
                    {
                        ProductId = product.Id,
                        StoreId = store.Id,
                        CurrentStock = currentStock,
                        MinimumStock = minStock,
                        MaximumStock = minStock * 3,
                        AverageCost = product.PurchasePrice,
                        ImportBatchId = importBatch.Id
                    };

                    await _productStockRepository.AddAsync(productStock);
                    _logger.LogDebug("ProductStock creado para producto {Codigo}: Stock={Stock}, StockMin={StockMin}", codigo, currentStock, minStock);

                    result.ProcessedProducts++;
                    result.TotalStock += currentStock;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Fila {row}: Error al procesar - {ex.Message}");
                    result.SkippedProducts++;
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Error al leer el archivo Excel: {ex.Message}");
        }

        // Actualizar ImportBatch con estadísticas finales
        var endTime = DateTime.UtcNow;
        importBatch.TotalRecords = result.ProcessedProducts + result.SkippedProducts;
        importBatch.SuccessCount = result.ProcessedProducts;
        importBatch.ErrorCount = result.Errors.Count;
        
        // Actualizar ImportBatch con estadísticas
        await _importBatchRepository.UpdateAsync(importBatch);

        // Si se procesaron productos exitosamente, marcar la tienda como que ya tiene stock inicial
        if (result.ProcessedProducts > 0)
        {
            store.HasInitialStock = true;
            await _storeRepository.UpdateAsync(store);
            _logger.LogInformation("Tienda {StoreCode} marcada como con stock inicial cargado", storeCode);
        }

        // Actualizar resultado con información de batch
        result.TotalRecords = importBatch.TotalRecords;
        result.SuccessCount = importBatch.SuccessCount;
        result.SkippedCount = result.SkippedProducts;
        result.ErrorCount = importBatch.ErrorCount;
        result.ProcessingTime = endTime - startTime;

        _logger.LogInformation("Carga de stock finalizada. Procesados: {Processed}, Skipped: {Skipped}, Errores: {Errors}", result.ProcessedProducts, result.SkippedProducts, result.Errors.Count);

        return result;
    }

    public async Task<StockClearResultDto> ClearAllStockAsync()
    {
        var result = new StockClearResultDto();

        // Contar registros antes de eliminar
        var productStocks = await _productStockRepository.GetAllAsync();
        var movements = await _inventoryMovementRepository.GetAllAsync();

        result.ClearedProductStocks = productStocks.Count();
        result.ClearedMovements = movements.Count();

        // Limpiar InventoryMovements primero (por foreign keys)
        foreach (var movement in movements)
        {
            await _inventoryMovementRepository.DeleteAsync(movement.Id);
        }

        // Limpiar ProductStocks
        foreach (var stock in productStocks)
        {
            await _productStockRepository.DeleteAsync(stock.Id);
        }

        // Note: Product.Stock field has been removed - stock is now managed only in ProductStocks table
        result.ResetProducts = 0;

        return result;
    }

    public async Task<StoreClearResultDto> ClearStockByStoreAsync(string storeCode)
    {
        var result = new StoreClearResultDto { StoreCode = storeCode };

        // Validar store
        var store = await _storeRepository.GetByCodeAsync(storeCode);
        if (store == null)
        {
            throw new ArgumentException($"No se encontró el almacén con código: {storeCode}");
        }

        result.StoreName = store.Name;

        // Obtener ProductStocks y InventoryMovements de este store
        var storeProductStocks = await _productStockRepository.GetByStoreIdAsync(store.Id);
        var storeMovements = (await _inventoryMovementRepository.GetAllAsync())
            .Where(m => m.StoreId == store.Id);

        result.ClearedProductStocks = storeProductStocks.Count();
        result.ClearedMovements = storeMovements.Count();

        // Limpiar InventoryMovements de este store primero
        foreach (var movement in storeMovements)
        {
            await _inventoryMovementRepository.DeleteAsync(movement.Id);
        }

        // Limpiar ProductStocks de este store
        foreach (var stock in storeProductStocks)
        {
            await _productStockRepository.DeleteAsync(stock.Id);
        }

        return result;
    }

    public async Task<StockSummaryDto> GetStockSummaryAsync()
    {
        var summary = new StockSummaryDto();
        
        var products = await _productRepository.GetAllAsync();
        var stores = await _storeRepository.GetAllAsync();
        var productStocks = await _productStockRepository.GetAllAsync();

        summary.TotalProducts = products.Count();
        summary.ProductsWithStock = productStocks.Where(ps => ps.CurrentStock > 0).Select(ps => ps.ProductId).Distinct().Count();
        summary.ProductsWithoutStock = summary.TotalProducts - summary.ProductsWithStock;

        foreach (var store in stores)
        {
            var storeStocks = productStocks.Where(ps => ps.StoreId == store.Id);
            
            var storeSummary = new StoreStockSummaryDto
            {
                StoreId = store.Id,
                StoreCode = store.Code,
                StoreName = store.Name,
                ProductsWithStock = storeStocks.Count(ps => ps.CurrentStock > 0),
                TotalStock = storeStocks.Sum(ps => ps.CurrentStock),
                ProductsUnderMinimum = storeStocks.Count(ps => ps.CurrentStock < ps.MinimumStock && ps.MinimumStock > 0)
            };

            summary.Stores.Add(storeSummary);
        }

        return summary;
    }

    public async Task<int> DeleteProductStocksByBatchIdAsync(int batchId)
    {
        var deletedCount = 0;
        
        // Obtener todos los ProductStocks de este batch
        var productStocks = await _productStockRepository.GetAllAsync();
        var batchProductStocks = productStocks.Where(ps => ps.ImportBatchId == batchId).ToList();
        
        // Eliminar cada ProductStock
        foreach (var productStock in batchProductStocks)
        {
            await _productStockRepository.DeleteAsync(productStock.Id);
            deletedCount++;
        }
        
        return deletedCount;
    }

    private async Task<StockColumnMapping> GetColumnMappingAsync()
    {
        try
        {
            var configKey = "IMPORT_MAPPING_STOCK";
            var mappingJson = await _configRepository.GetConfigValueAsync(configKey);
            
            if (string.IsNullOrEmpty(mappingJson))
            {
                _logger.LogWarning("No stock column mapping configuration found, using defaults");
                return new StockColumnMapping();
            }

            var mapping = System.Text.Json.JsonSerializer.Deserialize<StockColumnMapping>(mappingJson);
            return mapping ?? new StockColumnMapping();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving stock column mapping configuration, using defaults");
            return new StockColumnMapping();
        }
    }
}