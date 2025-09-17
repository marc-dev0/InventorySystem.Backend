using InventorySystem.Application.DTOs;
using InventorySystem.Application.Interfaces;
using InventorySystem.Core.Entities;
using InventorySystem.Core.Interfaces;
using InventorySystem.Application.Services;

namespace InventorySystem.Application.Services;

public class InventoryService : IInventoryService
{
    private readonly IProductStockRepository _productStockRepository;
    private readonly ConfigurationService _configurationService;

    public InventoryService(IProductStockRepository productStockRepository, ConfigurationService configurationService)
    {
        _productStockRepository = productStockRepository;
        _configurationService = configurationService;
    }

    public async Task<PaginatedResponseDto<InventoryItemDto>> GetPaginatedAsync(int page, int pageSize, string search = "", string storeCode = "", bool? lowStock = null)
    {
        // If no lowStock filter, use direct repository call for better performance
        if (lowStock != true)
        {
            var (items, totalCount) = await _productStockRepository.GetPaginatedAsync(page, pageSize, search, storeCode);
            var inventoryItems = new List<InventoryItemDto>();
            foreach (var item in items)
            {
                var dto = await MapToDtoAsync(item);
                inventoryItems.Add(dto);
            }

            return new PaginatedResponseDto<InventoryItemDto>
            {
                Data = inventoryItems,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };
        }

        // For lowStock filter, get all items and apply filter (similar to ProductService logic)
        var (allItems, _) = await _productStockRepository.GetPaginatedAsync(1, int.MaxValue, search, storeCode);
        var allInventoryItems = new List<InventoryItemDto>();
        foreach (var item in allItems)
        {
            var dto = await MapToDtoAsync(item);
            allInventoryItems.Add(dto);
        }

        // Apply lowStock filter
        var filteredItems = allInventoryItems.Where(item => item.IsLowStock).ToList();

        // Apply pagination to filtered results
        var paginatedItems = filteredItems
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PaginatedResponseDto<InventoryItemDto>
        {
            Data = paginatedItems,
            TotalCount = filteredItems.Count,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)filteredItems.Count / pageSize)
        };
    }

    public async Task<IEnumerable<InventoryItemDto>> GetAllAsync()
    {
        var items = await _productStockRepository.GetAllAsync();
        var inventoryItems = new List<InventoryItemDto>();
        foreach (var item in items)
        {
            var dto = await MapToDtoAsync(item);
            inventoryItems.Add(dto);
        }
        return inventoryItems;
    }

    public async Task<IEnumerable<InventoryItemDto>> GetByStoreAsync(int storeId)
    {
        var items = await _productStockRepository.GetByStoreIdAsync(storeId);
        var inventoryItems = new List<InventoryItemDto>();
        foreach (var item in items)
        {
            var dto = await MapToDtoAsync(item);
            inventoryItems.Add(dto);
        }
        return inventoryItems;
    }

    public async Task<IEnumerable<InventoryItemDto>> GetLowStockItemsAsync()
    {
        var allItems = await _productStockRepository.GetAllAsync();
        var inventoryItems = new List<InventoryItemDto>();
        foreach (var item in allItems)
        {
            var dto = await MapToDtoAsync(item);
            if (dto.IsLowStock)
            {
                inventoryItems.Add(dto);
            }
        }
        return inventoryItems;
    }

    public async Task<object> GetInventoryStatsAsync(string search = "", string storeCode = "", bool? lowStock = null)
    {
        // Get all items matching the search and store filters
        var (allItems, _) = await _productStockRepository.GetPaginatedAsync(1, int.MaxValue, search, storeCode);
        var allInventoryItems = new List<InventoryItemDto>();
        foreach (var item in allItems)
        {
            var dto = await MapToDtoAsync(item);
            allInventoryItems.Add(dto);
        }

        // Apply lowStock filter if specified for accurate stats
        var itemsToAnalyze = allInventoryItems;
        if (lowStock == true)
        {
            itemsToAnalyze = allInventoryItems.Where(item => item.IsLowStock).ToList();
        }

        var totalItems = itemsToAnalyze.Count;
        var lowStockItems = itemsToAnalyze.Count(item => item.IsLowStock);
        var outOfStockItems = itemsToAnalyze.Count(item => item.CurrentStock <= 0);
        var totalValue = itemsToAnalyze.Sum(item => item.TotalValue);

        return new
        {
            TotalItems = totalItems,
            LowStockItems = lowStockItems,
            OutOfStockItems = outOfStockItems,
            TotalValue = totalValue
        };
    }

    private async Task<InventoryItemDto> MapToDtoAsync(ProductStock productStock)
    {
        var totalValue = productStock.CurrentStock * productStock.AverageCost;

        // Use global minimum stock configuration for low stock determination (same as ProductService)
        var globalMinimumStock = await _configurationService.GetGlobalMinimumStockAsync();
        var effectiveMinimumStock = productStock.MinimumStock > 0 ? productStock.MinimumStock : globalMinimumStock;
        var isLowStock = productStock.CurrentStock <= effectiveMinimumStock;

        return new InventoryItemDto
        {
            Id = productStock.Id,
            ProductCode = productStock.Product?.Code ?? string.Empty,
            ProductName = productStock.Product?.Name ?? string.Empty,
            StoreName = productStock.Store?.Name ?? string.Empty,
            StoreCode = productStock.Store?.Code ?? string.Empty,
            CurrentStock = productStock.CurrentStock,
            MinimumStock = productStock.MinimumStock,
            MaximumStock = productStock.MaximumStock,
            AverageCost = productStock.AverageCost,
            TotalValue = totalValue,
            IsLowStock = isLowStock,
            ProductId = productStock.ProductId,
            StoreId = productStock.StoreId
        };
    }
}