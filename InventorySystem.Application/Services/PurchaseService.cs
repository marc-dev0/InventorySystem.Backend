using InventorySystem.Application.DTOs;
using InventorySystem.Application.Interfaces;
using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Entities;

namespace InventorySystem.Application.Services;

public class PurchaseService : IPurchaseService
{
    private readonly IPurchaseRepository _purchaseRepository;
    private readonly IProductRepository _productRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IProductStockRepository _productStockRepository;

    public PurchaseService(
        IPurchaseRepository purchaseRepository,
        IProductRepository productRepository,
        ISupplierRepository supplierRepository,
        IStoreRepository storeRepository,
        IProductStockRepository productStockRepository)
    {
        _purchaseRepository = purchaseRepository;
        _productRepository = productRepository;
        _supplierRepository = supplierRepository;
        _storeRepository = storeRepository;
        _productStockRepository = productStockRepository;
    }

    public async Task<IEnumerable<PurchaseDto>> GetAllAsync()
    {
        var purchases = await _purchaseRepository.GetAllAsync();
        return purchases.Select(MapToDto);
    }

    public async Task<PurchaseDto?> GetByIdAsync(int id)
    {
        var purchase = await _purchaseRepository.GetByIdAsync(id);
        return purchase != null ? MapToDto(purchase) : null;
    }

    public async Task<PurchaseDetailsDto?> GetPurchaseDetailsAsync(int id)
    {
        var purchase = await _purchaseRepository.GetPurchaseDetailsAsync(id);
        return purchase != null ? MapToDetailsDto(purchase) : null;
    }

    public async Task<IEnumerable<PurchaseDto>> GetPurchasesByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var purchases = await _purchaseRepository.GetPurchasesByDateRangeAsync(startDate, endDate);
        return purchases.Select(MapToDto);
    }

    public async Task<PurchaseDto> CreateAsync(CreatePurchaseDto dto)
    {
        var purchaseNumber = await _purchaseRepository.GeneratePurchaseNumberAsync();
        
        var purchase = new Purchase
        {
            PurchaseNumber = purchaseNumber,
            PurchaseDate = DateTime.Now,
            Notes = dto.Notes,
            Details = new List<PurchaseDetail>()
        };

        decimal subTotal = 0;
        
        foreach (var detailDto in dto.Details)
        {
            var product = await _productRepository.GetByIdAsync(detailDto.ProductId)
                ?? throw new InvalidOperationException($"Producto con ID {detailDto.ProductId} no encontrado");

            var supplier = await _supplierRepository.GetByIdAsync(detailDto.SupplierId)
                ?? throw new InvalidOperationException($"Proveedor con ID {detailDto.SupplierId} no encontrado");

            var detail = new PurchaseDetail
            {
                ProductId = detailDto.ProductId,
                SupplierId = detailDto.SupplierId,
                Quantity = detailDto.Quantity,
                UnitPrice = detailDto.UnitPrice,
                Subtotal = detailDto.Quantity * detailDto.UnitPrice
            };

            purchase.Details.Add(detail);
            subTotal += detail.Subtotal;

            // Update product stock in ProductStocks table
            // TODO: This method needs to be updated to receive storeId parameter
            // For now, get the first store (this should be fixed)
            var stores = await _storeRepository.GetAllAsync();
            var defaultStore = stores.FirstOrDefault();
            if (defaultStore != null)
            {
                var productStock = await GetOrCreateProductStockAsync(product.Id, defaultStore.Id);
                productStock.CurrentStock += detailDto.Quantity;
                await _productStockRepository.UpdateAsync(productStock);
            }
        }

        purchase.SubTotal = subTotal;
        purchase.Taxes = subTotal * 0.19m; // 19% tax
        purchase.Total = purchase.SubTotal + purchase.Taxes;

        var createdPurchase = await _purchaseRepository.AddAsync(purchase);
        return MapToDto(createdPurchase);
    }

    public async Task<object> GetPurchasesReportAsync(DateTime startDate, DateTime endDate)
    {
        var purchases = await _purchaseRepository.GetPurchasesByDateRangeAsync(startDate, endDate);
        
        return new
        {
            DateRange = new { StartDate = startDate, EndDate = endDate },
            TotalPurchases = purchases.Count(),
            TotalAmount = purchases.Sum(p => p.Total),
            AverageAmount = purchases.Any() ? purchases.Average(p => p.Total) : 0,
            Purchases = purchases.Select(MapToDto)
        };
    }

    private static PurchaseDto MapToDto(Purchase purchase)
    {
        return new PurchaseDto
        {
            Id = purchase.Id,
            PurchaseNumber = purchase.PurchaseNumber,
            PurchaseDate = purchase.PurchaseDate,
            SubTotal = purchase.SubTotal,
            Taxes = purchase.Taxes,
            Total = purchase.Total,
            Notes = purchase.Notes,
            ItemCount = purchase.Details?.Count ?? 0
        };
    }

    private static PurchaseDetailsDto MapToDetailsDto(Purchase purchase)
    {
        return new PurchaseDetailsDto
        {
            Id = purchase.Id,
            PurchaseNumber = purchase.PurchaseNumber,
            PurchaseDate = purchase.PurchaseDate,
            SubTotal = purchase.SubTotal,
            Taxes = purchase.Taxes,
            Total = purchase.Total,
            Notes = purchase.Notes,
            ItemCount = purchase.Details?.Count ?? 0,
            Details = purchase.Details?.Select(d => new PurchaseDetailDto
            {
                Id = d.Id,
                ProductId = d.ProductId,
                ProductName = d.Product?.Name ?? string.Empty,
                ProductCode = d.Product?.Code ?? string.Empty,
                SupplierId = d.SupplierId ?? 0,
                SupplierName = d.Supplier?.Name ?? string.Empty,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                Subtotal = d.Subtotal
            }).ToList() ?? new List<PurchaseDetailDto>()
        };
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
}