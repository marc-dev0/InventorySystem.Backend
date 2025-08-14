using ClosedXML.Excel;
using InventorySystem.Application.Interfaces;
using InventorySystem.Application.DTOs;
using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Entities;

namespace InventorySystem.Application.Services;

public class StockInitialService : IStockInitialService
{
    private readonly IProductRepository _productRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IProductStockRepository _productStockRepository;
    private readonly IInventoryMovementRepository _inventoryMovementRepository;

    public StockInitialService(
        IProductRepository productRepository,
        IStoreRepository storeRepository,
        IProductStockRepository productStockRepository,
        IInventoryMovementRepository inventoryMovementRepository)
    {
        _productRepository = productRepository;
        _storeRepository = storeRepository;
        _productStockRepository = productStockRepository;
        _inventoryMovementRepository = inventoryMovementRepository;
    }

    public async Task<StockLoadResultDto> LoadStockFromExcelAsync(Stream excelStream, string fileName, string storeCode)
    {
        var result = new StockLoadResultDto { StoreCode = storeCode };

        // Validar store
        var store = await _storeRepository.GetByCodeAsync(storeCode);
        if (store == null)
        {
            result.Errors.Add($"No se encontró el almacén con código: {storeCode}");
            return result;
        }

        result.StoreName = store.Name;

        // Obtener todos los productos existentes
        var products = await _productRepository.GetAllAsync();
        var productsDict = products.ToDictionary(p => p.Code, p => p);

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
            
            // Procesar desde la fila 2 (asumiendo header en fila 1)
            for (int row = 2; row <= lastRow; row++)
            {
                try
                {
                    var codigo = worksheet.Cell(row, 2).GetString().Trim(); // Columna B - Código
                    var stockStr = worksheet.Cell(row, 11).GetString().Trim(); // Columna K - Stock
                    var stockMinStr = worksheet.Cell(row, 12).GetString().Trim(); // Columna L - Stock min

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
                    int currentStock = 0;
                    int minStock = 0;

                    if (!string.IsNullOrEmpty(stockStr) && !int.TryParse(stockStr, out currentStock))
                    {
                        result.Warnings.Add($"Fila {row}: Stock inválido para producto {codigo}: {stockStr}");
                    }

                    if (!string.IsNullOrEmpty(stockMinStr) && !int.TryParse(stockMinStr, out minStock))
                    {
                        result.Warnings.Add($"Fila {row}: Stock mínimo inválido para producto {codigo}: {stockMinStr}");
                    }

                    // Verificar si ya existe ProductStock para este producto y store
                    var existingStock = await _productStockRepository.GetByProductAndStoreAsync(product.Id, store.Id);
                    
                    if (existingStock != null)
                    {
                        // Skip existing stock - no update (incremental load)
                        result.SkippedProducts++;
                        continue;
                    }
                    
                    // Crear nuevo ProductStock
                    var productStock = new ProductStock
                    {
                        ProductId = product.Id,
                        StoreId = store.Id,
                        CurrentStock = currentStock,
                        MinimumStock = minStock,
                        MaximumStock = minStock * 3,
                        AverageCost = product.PurchasePrice
                    };

                    await _productStockRepository.AddAsync(productStock);

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

        // Resetear stock en Products (campo legacy)
        var products = await _productRepository.GetAllAsync();
        foreach (var product in products)
        {
            product.Stock = 0;
            await _productRepository.UpdateAsync(product);
        }

        result.ResetProducts = products.Count();

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
}