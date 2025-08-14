using InventorySystem.Application.Interfaces;
using InventorySystem.Application.DTOs;
using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Entities;

namespace InventorySystem.Application.Services;

public class SalesImportTrackingService : ISalesImportTrackingService
{
    private readonly ISaleRepository _saleRepository;
    private readonly IInventoryMovementRepository _inventoryMovementRepository;
    private readonly IProductStockRepository _productStockRepository;
    private readonly IImportBatchRepository _importBatchRepository;
    private readonly IProductRepository _productRepository;

    public SalesImportTrackingService(
        ISaleRepository saleRepository,
        IInventoryMovementRepository inventoryMovementRepository,
        IProductStockRepository productStockRepository,
        IImportBatchRepository importBatchRepository,
        IProductRepository productRepository)
    {
        _saleRepository = saleRepository;
        _inventoryMovementRepository = inventoryMovementRepository;
        _productStockRepository = productStockRepository;
        _importBatchRepository = importBatchRepository;
        _productRepository = productRepository;
    }

    public async Task<List<ImportBatchDto>> GetRecentImportsAsync(int days = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);
        var batches = await _importBatchRepository.GetActiveBatchesAsync();
        
        var recentBatches = batches
            .Where(b => b.ImportDate >= cutoffDate)
            .Select(b => new ImportBatchDto
            {
                Id = b.Id,
                BatchCode = b.BatchCode,
                BatchType = b.BatchType,
                FileName = b.FileName,
                StoreCode = b.StoreCode,
                TotalRecords = b.TotalRecords,
                SuccessCount = b.SuccessCount,
                SkippedCount = b.SkippedCount,
                ErrorCount = b.ErrorCount,
                ImportDate = b.ImportDate,
                ImportedBy = b.ImportedBy,
                IsDeleted = b.IsDeleted,
                DeletedAt = b.DeletedAt,
                DeletedBy = b.DeletedBy,
                DeleteReason = b.DeleteReason
            })
            .OrderByDescending(b => b.ImportDate)
            .ToList();

        return recentBatches;
    }

    public async Task<SalesDeleteResultDto> DeleteSalesImportAsync(string batchCode, string deletedBy)
    {
        // Buscar el batch por código
        var importBatch = await _importBatchRepository.GetBatchByCodeAsync(batchCode);
        
        if (importBatch == null || importBatch.IsDeleted)
        {
            throw new InvalidOperationException($"Import batch with code '{batchCode}' not found or already deleted.");
        }

        var result = new SalesDeleteResultDto
        {
            ImportedAt = importBatch.ImportDate,
            ImportSource = batchCode,
            DeletedBy = deletedBy,
            DeletedAt = DateTime.UtcNow
        };

        // Buscar ventas de este batch específico
        var sales = await _saleRepository.GetAllAsync();
        var targetSales = sales.Where(s => s.ImportBatchId == importBatch.Id).ToList();

        if (!targetSales.Any())
        {
            // Marcar el batch como eliminado aunque no tenga ventas
            importBatch.IsDeleted = true;
            importBatch.DeletedAt = DateTime.UtcNow;
            importBatch.DeletedBy = deletedBy;
            importBatch.DeleteReason = "Batch deleted - no sales found";
            await _importBatchRepository.UpdateAsync(importBatch);
            
            return result;
        }

        var affectedProductCodes = new HashSet<string>();
        var totalRevertedAmount = 0m;

        // Obtener todos los números de documento de las ventas de este batch
        var saleNumbers = targetSales.Select(s => s.SaleNumber).ToList();

        // 1. Buscar y revertir TODOS los movimientos de este batch directamente
        var allMovements = await _inventoryMovementRepository.GetAllAsync();
        var batchMovements = allMovements.Where(m => 
            m.Type == MovementType.Sale &&
            m.DocumentNumber != null &&
            saleNumbers.Contains(m.DocumentNumber)
        ).ToList();

        foreach (var movement in batchMovements)
        {
            // Revertir el movimiento: devolver el stock
            if (movement.ProductStockId.HasValue)
            {
                var productStock = await _productStockRepository.GetByIdAsync(movement.ProductStockId.Value);
                if (productStock != null)
                {
                    productStock.CurrentStock += Math.Abs(movement.Quantity);
                    await _productStockRepository.UpdateAsync(productStock);
                    
                    // También actualizar el Product.Stock legacy
                    var product = await _productRepository.GetByIdAsync(productStock.ProductId);
                    if (product != null)
                    {
                        product.Stock += Math.Abs(movement.Quantity);
                        await _productRepository.UpdateAsync(product);
                        affectedProductCodes.Add(product.Code);
                    }
                }
            }
            
            // Eliminar el movimiento
            await _inventoryMovementRepository.DeleteAsync(movement.Id);
            result.RevertedMovements++;
            
            // Sumar al total revertido
            totalRevertedAmount += Math.Abs(movement.TotalCost ?? (movement.UnitCost ?? 0) * Math.Abs(movement.Quantity));
        }

        // Contar detalles de ventas para estadísticas
        foreach (var sale in targetSales)
        {
            // Contar detalles aproximadamente (sin cargar navigation)
            var saleDetailCount = allMovements.Count(m => m.DocumentNumber == sale.SaleNumber);
            result.DeletedSaleDetails += saleDetailCount;
        }

        // 2. Eliminar las ventas y sus detalles
        foreach (var sale in targetSales)
        {
            await _saleRepository.DeleteAsync(sale.Id);
            result.DeletedSales++;
        }

        // 3. Marcar el batch como eliminado
        importBatch.IsDeleted = true;
        importBatch.DeletedAt = DateTime.UtcNow;
        importBatch.DeletedBy = deletedBy;
        importBatch.DeleteReason = $"Sales batch deleted - {result.DeletedSales} sales removed";
        await _importBatchRepository.UpdateAsync(importBatch);

        result.AffectedProducts = affectedProductCodes.Count;
        result.UpdatedProductCodes = affectedProductCodes.ToList();
        result.TotalRevertedAmount = totalRevertedAmount;

        return result;
    }

    public async Task<ImportBatchDto?> GetBatchByCodeAsync(string batchCode)
    {
        var batch = await _importBatchRepository.GetBatchByCodeAsync(batchCode);
        
        if (batch == null)
            return null;
            
        return new ImportBatchDto
        {
            Id = batch.Id,
            BatchCode = batch.BatchCode,
            BatchType = batch.BatchType,
            FileName = batch.FileName,
            StoreCode = batch.StoreCode,
            TotalRecords = batch.TotalRecords,
            SuccessCount = batch.SuccessCount,
            SkippedCount = batch.SkippedCount,
            ErrorCount = batch.ErrorCount,
            ImportDate = batch.ImportDate,
            ImportedBy = batch.ImportedBy,
            IsDeleted = batch.IsDeleted,
            DeletedAt = batch.DeletedAt,
            DeletedBy = batch.DeletedBy,
            DeleteReason = batch.DeleteReason
        };
    }
}