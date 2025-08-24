using InventorySystem.Application.DTOs;
using InventorySystem.Application.Interfaces;
using InventorySystem.Core.Entities;
using InventorySystem.Core.Interfaces;

namespace InventorySystem.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;

    public ProductService(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsync()
    {
        var products = await _productRepository.GetAllAsync();
        return products.Select(MapToDto);
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        return product != null ? MapToDto(product) : null;
    }

    public async Task<ProductDto?> GetByCodeAsync(string code)
    {
        var product = await _productRepository.GetByCodeAsync(code);
        return product != null ? MapToDto(product) : null;
    }

    public async Task<IEnumerable<ProductDto>> GetByCategoryAsync(int categoryId)
    {
        var products = await _productRepository.GetByCategoryAsync(categoryId);
        return products.Select(MapToDto);
    }

    public async Task<IEnumerable<ProductDto>> GetLowStockProductsAsync()
    {
        var products = await _productRepository.GetLowStockProductsAsync();
        return products.Select(MapToDto);
    }

    public async Task<IEnumerable<ProductDto>> SearchProductsAsync(string searchTerm)
    {
        var products = await _productRepository.SearchProductsAsync(searchTerm);
        return products.Select(MapToDto);
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto)
    {
        // Validate that code doesn't exist
        if (await _productRepository.CodeExistsAsync(dto.Code))
        {
            throw new ArgumentException($"Product with code {dto.Code} already exists");
        }

        // Validate that category exists
        var category = await _categoryRepository.GetByIdAsync(dto.CategoryId);
        if (category == null)
        {
            throw new ArgumentException("The specified category does not exist");
        }

        var product = new Product
        {
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            PurchasePrice = dto.PurchasePrice,
            SalePrice = dto.SalePrice,
            Stock = dto.Stock,
            MinimumStock = dto.MinimumStock,
            Unit = dto.Unit,
            CategoryId = dto.CategoryId,
            SupplierId = dto.SupplierId,
            Active = true
        };

        var createdProduct = await _productRepository.AddAsync(product);
        return MapToDto(createdProduct);
    }

    public async Task UpdateAsync(int id, UpdateProductDto dto)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null)
        {
            throw new KeyNotFoundException("Product not found");
        }

        // Validate unique code
        if (await _productRepository.CodeExistsAsync(dto.Code, id))
        {
            throw new ArgumentException($"Another product with code {dto.Code} already exists");
        }

        product.Code = dto.Code;
        product.Name = dto.Name;
        product.Description = dto.Description;
        product.PurchasePrice = dto.PurchasePrice;
        product.SalePrice = dto.SalePrice;
        product.MinimumStock = dto.MinimumStock;
        product.Unit = dto.Unit;
        product.Active = dto.Active;
        product.CategoryId = dto.CategoryId;
        product.SupplierId = dto.SupplierId;

        await _productRepository.UpdateAsync(product);
    }

    public async Task DeleteAsync(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null)
        {
            throw new KeyNotFoundException("Product not found");
        }

        product.IsDeleted = true;
        await _productRepository.UpdateAsync(product);
    }

    public async Task UpdateStockAsync(int id, decimal newStock, string? reason)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null)
        {
            throw new KeyNotFoundException("Product not found");
        }

        await _productRepository.UpdateStockAsync(id, newStock);
    }

    private static ProductDto MapToDto(Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            Code = product.Code,
            Name = product.Name,
            Description = product.Description,
            PurchasePrice = product.PurchasePrice,
            SalePrice = product.SalePrice,
            Stock = product.Stock,
            MinimumStock = product.MinimumStock,
            Unit = product.Unit,
            Active = product.Active,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name ?? string.Empty,
            SupplierId = product.SupplierId,
            SupplierName = product.Supplier?.Name,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }
}
