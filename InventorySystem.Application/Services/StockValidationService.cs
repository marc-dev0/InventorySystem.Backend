using InventorySystem.Application.DTOs;
using InventorySystem.Application.Interfaces;
using InventorySystem.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace InventorySystem.Application.Services;

public class StockValidationService : IStockValidationService
{
    private readonly IProductRepository _productRepository;
    private readonly IProductStockRepository _productStockRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly ILogger<StockValidationService> _logger;

    public StockValidationService(
        IProductRepository productRepository,
        IProductStockRepository productStockRepository,
        IStoreRepository storeRepository,
        ILogger<StockValidationService> logger)
    {
        _productRepository = productRepository;
        _productStockRepository = productStockRepository;
        _storeRepository = storeRepository;
        _logger = logger;
    }

    public async Task<StockValidationResultDto> ValidateSalesStockImpactAsync(List<TandiaSaleDetailDto> salesData, string storeCode)
    {
        var result = new StockValidationResultDto
        {
            ValidationDate = DateTime.UtcNow
        };

        try
        {
            // Verificar que la tienda existe
            var store = await _storeRepository.FirstOrDefaultAsync(s => s.Code == storeCode);
            if (store == null)
            {
                result.Issues.Add(new StockIssueDto
                {
                    StoreCode = storeCode,
                    IssueType = StockIssueType.StoreNotFound,
                    Description = $"La tienda con código '{storeCode}' no existe",
                    Recommendation = "Verificar el código de tienda"
                });
                result.IsValid = false;
                result.CriticalIssues = 1;
                return result;
            }

            // Simular el impacto de todas las ventas
            var stockSimulations = await SimulateStockChangesAsync(salesData, storeCode);
            
            var lineNumber = 1;
            foreach (var sale in salesData)
            {
                lineNumber++;
                
                // Verificar si el producto existe
                var product = await _productRepository.FirstOrDefaultAsync(p => p.Code == sale.ProductCode);
                if (product == null)
                {
                    result.Issues.Add(new StockIssueDto
                    {
                        ProductCode = sale.ProductCode,
                        StoreCode = storeCode,
                        RequestedQuantity = (int)sale.Quantity,
                        IssueType = StockIssueType.ProductNotFound,
                        Description = $"Producto '{sale.ProductCode}' no encontrado",
                        Recommendation = "Verificar el código del producto o importar productos primero",
                        SaleLineNumber = lineNumber,
                        DocumentNumber = sale.DocumentNumber
                    });
                    result.CriticalIssues++;
                    continue;
                }

                // Obtener stock actual
                var currentStock = await GetCurrentStockAsync(sale.ProductCode, storeCode);
                var stockAfterSale = currentStock - (int)(int)sale.Quantity;

                // Validar diferentes tipos de problemas
                if (stockAfterSale < 0)
                {
                    result.Issues.Add(new StockIssueDto
                    {
                        ProductCode = sale.ProductCode,
                        ProductName = product.Name,
                        StoreCode = storeCode,
                        CurrentStock = currentStock,
                        RequestedQuantity = (int)sale.Quantity,
                        ResultingStock = stockAfterSale,
                        IssueType = StockIssueType.NegativeStock,
                        Description = $"La venta causaría stock negativo: {stockAfterSale}",
                        Recommendation = $"Reducir cantidad o verificar stock. Disponible: {currentStock}",
                        SaleLineNumber = lineNumber,
                        DocumentNumber = sale.DocumentNumber
                    });
                    result.CriticalIssues++;
                }
                else if (currentStock == 0)
                {
                    result.Issues.Add(new StockIssueDto
                    {
                        ProductCode = sale.ProductCode,
                        ProductName = product.Name,
                        StoreCode = storeCode,
                        CurrentStock = currentStock,
                        RequestedQuantity = (int)sale.Quantity,
                        ResultingStock = stockAfterSale,
                        IssueType = StockIssueType.ZeroStock,
                        Description = "El producto no tiene stock disponible",
                        Recommendation = "Verificar movimientos de stock o realizar reposición",
                        SaleLineNumber = lineNumber,
                        DocumentNumber = sale.DocumentNumber
                    });
                    result.CriticalIssues++;
                }
                else if (stockAfterSale <= 5) // Warning si queda poco stock
                {
                    result.Issues.Add(new StockIssueDto
                    {
                        ProductCode = sale.ProductCode,
                        ProductName = product.Name,
                        StoreCode = storeCode,
                        CurrentStock = currentStock,
                        RequestedQuantity = (int)sale.Quantity,
                        ResultingStock = stockAfterSale,
                        IssueType = StockIssueType.LowStockWarning,
                        Description = $"Stock bajo después de la venta: {stockAfterSale}",
                        Recommendation = "Considerar reposición del producto",
                        SaleLineNumber = lineNumber,
                        DocumentNumber = sale.DocumentNumber
                    });
                    result.WarningIssues++;
                }
            }

            // Calcular estadísticas
            result.TotalProducts = salesData.Select(s => s.ProductCode).Distinct().Count();
            result.ProductsWithIssues = result.Issues.Select(i => i.ProductCode).Distinct().Count();
            result.IsValid = result.CriticalIssues == 0;
            
            // Generar resumen
            if (result.IsValid)
            {
                result.ValidationSummary = $"Validación exitosa: {result.TotalProducts} productos validados sin problemas críticos.";
                if (result.WarningIssues > 0)
                {
                    result.ValidationSummary += $" {result.WarningIssues} advertencias de stock bajo.";
                }
            }
            else
            {
                result.ValidationSummary = $"Validación falló: {result.CriticalIssues} errores críticos y {result.WarningIssues} advertencias encontradas.";
            }

            _logger.LogInformation($"Stock validation completed: {result.CriticalIssues} critical issues, {result.WarningIssues} warnings");
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during stock validation");
            result.IsValid = false;
            result.Issues.Add(new StockIssueDto
            {
                Description = $"Error durante la validación: {ex.Message}",
                IssueType = StockIssueType.ProductNotFound,
                Recommendation = "Contactar soporte técnico"
            });
            return result;
        }
    }

    public async Task<StockValidationResultDto> ValidateCurrentStockLevelsAsync(string storeCode)
    {
        var result = new StockValidationResultDto
        {
            ValidationDate = DateTime.UtcNow
        };

        try
        {
            var store = await _storeRepository.FirstOrDefaultAsync(s => s.Code == storeCode);
            if (store == null)
            {
                result.Issues.Add(new StockIssueDto
                {
                    StoreCode = storeCode,
                    IssueType = StockIssueType.StoreNotFound,
                    Description = $"La tienda con código '{storeCode}' no existe"
                });
                result.IsValid = false;
                return result;
            }

            var allProductStocks = await _productStockRepository.GetAllAsync();
            var storeStocks = allProductStocks.Where(ps => ps.StoreId == store.Id).ToList();

            result.TotalProducts = storeStocks.Count;

            foreach (var productStock in storeStocks)
            {
                var product = await _productRepository.GetByIdAsync(productStock.ProductId);
                
                if (productStock.CurrentStock < 0)
                {
                    result.Issues.Add(new StockIssueDto
                    {
                        ProductCode = product?.Code ?? "UNKNOWN",
                        ProductName = product?.Name ?? "UNKNOWN",
                        StoreCode = storeCode,
                        CurrentStock = (int)productStock.CurrentStock,
                        IssueType = StockIssueType.NegativeStock,
                        Description = $"Stock negativo detectado: {productStock.CurrentStock}",
                        Recommendation = "Revisar movimientos de inventario y corregir stock"
                    });
                    result.CriticalIssues++;
                }
                else if (productStock.CurrentStock == 0)
                {
                    result.Issues.Add(new StockIssueDto
                    {
                        ProductCode = product?.Code ?? "UNKNOWN",
                        ProductName = product?.Name ?? "UNKNOWN",
                        StoreCode = storeCode,
                        CurrentStock = 0,
                        IssueType = StockIssueType.ZeroStock,
                        Description = "Producto sin stock",
                        Recommendation = "Considerar reposición"
                    });
                    result.WarningIssues++;
                }
                else if (productStock.CurrentStock <= 5)
                {
                    result.Issues.Add(new StockIssueDto
                    {
                        ProductCode = product?.Code ?? "UNKNOWN",
                        ProductName = product?.Name ?? "UNKNOWN",
                        StoreCode = storeCode,
                        CurrentStock = (int)productStock.CurrentStock,
                        IssueType = StockIssueType.LowStockWarning,
                        Description = $"Stock bajo: {productStock.CurrentStock}",
                        Recommendation = "Considerar reposición"
                    });
                    result.WarningIssues++;
                }
            }

            result.ProductsWithIssues = result.Issues.Select(i => i.ProductCode).Distinct().Count();
            result.IsValid = result.CriticalIssues == 0;
            result.ValidationSummary = $"Validación de {result.TotalProducts} productos: {result.CriticalIssues} errores críticos, {result.WarningIssues} advertencias";

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating current stock levels");
            result.IsValid = false;
            return result;
        }
    }

    public async Task<bool> ValidateSingleSaleStockAsync(string productCode, int quantity, string storeCode)
    {
        try
        {
            var currentStock = await GetCurrentStockAsync(productCode, storeCode);
            return currentStock >= quantity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error validating single sale stock for product {productCode}");
            return false;
        }
    }

    public async Task<int> GetCurrentStockAsync(string productCode, string storeCode)
    {
        try
        {
            var product = await _productRepository.FirstOrDefaultAsync(p => p.Code == productCode);
            if (product == null) return 0;

            var store = await _storeRepository.FirstOrDefaultAsync(s => s.Code == storeCode);
            if (store == null) return 0;

            var productStock = await _productStockRepository.GetByProductAndStoreAsync(product.Id, store.Id);
            return (int)(productStock?.CurrentStock ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting current stock for product {productCode} in store {storeCode}");
            return 0;
        }
    }

    public async Task<List<StockIssueDto>> SimulateStockChangesAsync(List<TandiaSaleDetailDto> salesData, string storeCode)
    {
        var issues = new List<StockIssueDto>();
        var stockSimulations = new Dictionary<string, int>(); // ProductCode -> Current simulated stock

        try
        {
            // Inicializar con stock actual
            var uniqueProducts = salesData.Select(s => s.ProductCode).Distinct();
            foreach (var productCode in uniqueProducts)
            {
                var currentStock = await GetCurrentStockAsync(productCode, storeCode);
                stockSimulations[productCode] = currentStock;
            }

            // Simular ventas cronológicamente
            var sortedSales = salesData.OrderBy(s => s.Date).ThenBy(s => s.DocumentNumber);
            var lineNumber = 1;

            foreach (var sale in sortedSales)
            {
                lineNumber++;
                
                if (!stockSimulations.ContainsKey(sale.ProductCode))
                {
                    stockSimulations[sale.ProductCode] = 0;
                }

                var currentSimulatedStock = stockSimulations[sale.ProductCode];
                var stockAfterSale = currentSimulatedStock - (int)sale.Quantity;
                
                if (stockAfterSale < 0)
                {
                    var product = await _productRepository.FirstOrDefaultAsync(p => p.Code == sale.ProductCode);
                    issues.Add(new StockIssueDto
                    {
                        ProductCode = sale.ProductCode,
                        ProductName = product?.Name ?? "UNKNOWN",
                        StoreCode = storeCode,
                        CurrentStock = currentSimulatedStock,
                        RequestedQuantity = (int)sale.Quantity,
                        ResultingStock = stockAfterSale,
                        IssueType = StockIssueType.NegativeStock,
                        Description = $"Simulación: Stock negativo en línea {lineNumber}",
                        Recommendation = "Revisar orden de ventas o stock inicial",
                        SaleLineNumber = lineNumber,
                        DocumentNumber = sale.DocumentNumber
                    });
                }

                // Actualizar stock simulado
                stockSimulations[sale.ProductCode] = stockAfterSale;
            }

            return issues;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during stock simulation");
            return issues;
        }
    }
}