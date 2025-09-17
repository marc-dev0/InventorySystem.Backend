using InventorySystem.Application.DTOs;
using InventorySystem.Application.Interfaces;
using InventorySystem.Core.Entities;
using InventorySystem.Core.Interfaces;

namespace InventorySystem.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductStockRepository _productStockRepository;
    private readonly ConfigurationService _configurationService;

    public ProductService(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        IProductStockRepository productStockRepository,
        ConfigurationService configurationService)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _productStockRepository = productStockRepository;
        _configurationService = configurationService;
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsync()
    {
        var products = await _productRepository.GetAllAsync();
        var productDtos = new List<ProductDto>();
        foreach (var product in products)
        {
            var dto = await MapToDtoAsync(product);
            productDtos.Add(dto);
        }
        return productDtos;
    }

    public async Task<PaginatedResponseDto<ProductDto>> GetPaginatedAsync(int page, int pageSize, string search = "", string categoryId = "", bool? lowStock = null, string status = "")
    {
        Console.WriteLine($"🔍 ProductService.GetPaginatedAsync called with: page={page}, pageSize={pageSize}, search='{search}', categoryId='{categoryId}', lowStock={lowStock}, status='{status}'");

        // If no advanced filters, use repository pagination for better performance
        if (lowStock != true && string.IsNullOrEmpty(status))
        {
            Console.WriteLine("📈 Using fast path (repository pagination) - no advanced filters");
            var (products, totalCount) = await _productRepository.GetPaginatedAsync(page, pageSize, search, categoryId);
            Console.WriteLine($"📊 Repository returned {products.Count()} products, totalCount={totalCount}");

            var productsDto = new List<ProductDto>();
            foreach (var product in products)
            {
                var dto = await MapToDtoAsync(product);
                productsDto.Add(dto);
            }

            Console.WriteLine($"✅ Returning {productsDto.Count} product DTOs");
            return new PaginatedResponseDto<ProductDto>
            {
                Data = productsDto,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };
        }

        // For advanced filters (lowStock, status), we need to get more products for filtering
        // Get a larger batch to ensure we have enough results after filtering
        var batchSize = Math.Max(pageSize * 10, 200); // Get at least 10 pages worth
        var currentBatch = 1;
        var filteredResults = new List<ProductDto>();
        var totalProcessed = 0;

        while (filteredResults.Count < pageSize && totalProcessed < 1000) // Safety limit
        {
            var (batchProducts, batchTotalCount) = await _productRepository.GetPaginatedAsync(currentBatch, batchSize, search, categoryId);

            if (!batchProducts.Any()) break; // No more products

            // Convert batch to DTOs
            var batchDtos = new List<ProductDto>();
            foreach (var product in batchProducts)
            {
                var dto = await MapToDtoAsync(product);
                batchDtos.Add(dto);
            }

            // Apply filters to this batch
            var batchFiltered = batchDtos.AsEnumerable();

            if (lowStock == true)
            {
                // Use global minimum stock configuration for low stock determination
                var globalMinimumStock = await _configurationService.GetGlobalMinimumStockAsync();

                batchFiltered = batchFiltered.Where(p =>
                {
                    // If product has specific minimum stock set, use that
                    if (p.MinimumStock > 0)
                    {
                        return p.CurrentStock <= p.MinimumStock;
                    }
                    // Otherwise, use global minimum stock configuration
                    return p.CurrentStock <= globalMinimumStock;
                });
            }

            if (!string.IsNullOrEmpty(status))
            {
                if (status == "active")
                    batchFiltered = batchFiltered.Where(p => p.Active);
                else if (status == "inactive")
                    batchFiltered = batchFiltered.Where(p => !p.Active);
            }

            filteredResults.AddRange(batchFiltered);
            totalProcessed += batchProducts.Count();

            if (batchProducts.Count() < batchSize) break; // Last batch
            currentBatch++;
        }

        // Calculate accurate total count for filtered results
        var actualTotal = await GetFilteredProductCountAsync(search, categoryId, lowStock, status);
        var estimatedTotal = actualTotal;

        // Apply pagination to filtered results
        var paginatedResults = filteredResults
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PaginatedResponseDto<ProductDto>
        {
            Data = paginatedResults,
            TotalCount = estimatedTotal,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)estimatedTotal / pageSize)
        };
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        return product != null ? await MapToDtoAsync(product) : null;
    }

    public async Task<ProductDto?> GetByCodeAsync(string code)
    {
        var product = await _productRepository.GetByCodeAsync(code);
        return product != null ? await MapToDtoAsync(product) : null;
    }

    public async Task<IEnumerable<ProductDto>> GetByCategoryAsync(int categoryId)
    {
        var products = await _productRepository.GetByCategoryAsync(categoryId);
        var productDtos = new List<ProductDto>();
        foreach (var product in products)
        {
            var dto = await MapToDtoAsync(product);
            productDtos.Add(dto);
        }
        return productDtos;
    }

    public async Task<IEnumerable<ProductDto>> GetLowStockProductsAsync()
    {
        var products = await _productRepository.GetLowStockProductsAsync();
        var productDtos = new List<ProductDto>();
        foreach (var product in products)
        {
            var dto = await MapToDtoAsync(product);
            productDtos.Add(dto);
        }
        return productDtos;
    }

    public async Task<IEnumerable<ProductDto>> SearchProductsAsync(string searchTerm)
    {
        var products = await _productRepository.SearchProductsAsync(searchTerm);
        var productDtos = new List<ProductDto>();
        foreach (var product in products)
        {
            var dto = await MapToDtoAsync(product);
            productDtos.Add(dto);
        }
        return productDtos;
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
        return await MapToDtoAsync(createdProduct);
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

    public async Task<object> GetProductStatsAsync(string search = "", string categoryId = "", bool? lowStock = null, string status = "")
    {
        // Use the service's own GetPaginatedAsync method to get filtered products with all filters applied
        var result = await GetPaginatedAsync(1, int.MaxValue, search, categoryId, lowStock, status);
        var productsDto = result.Data.ToList();

        // Calculate statistics based on filtered results
        var globalMinimumStock = await _configurationService.GetGlobalMinimumStockAsync();

        var totalProducts = productsDto.Count;
        var activeProducts = productsDto.Count(p => p.Active);
        var lowStockProducts = productsDto.Count(p =>
        {
            if (!p.Active) return false;
            var effectiveMinimumStock = p.MinimumStock > 0 ? p.MinimumStock : globalMinimumStock;
            return p.CurrentStock <= effectiveMinimumStock;
        });
        var outOfStockProducts = productsDto.Count(p => p.CurrentStock <= 0 && p.Active);
        var totalValue = productsDto.Where(p => p.Active).Sum(p => p.CurrentStock * p.SalePrice);

        return new
        {
            TotalProducts = totalProducts,
            ActiveProducts = activeProducts,
            LowStockProducts = lowStockProducts,
            OutOfStockProducts = outOfStockProducts,
            TotalValue = totalValue
        };
    }

    private async Task<ProductDto> MapToDtoAsync(Product product)
    {
        // Calculate real-time stock from ProductStocks across all stores
        var currentStock = await _productStockRepository.GetTotalStockForProductAsync(product.Id);

        return new ProductDto
        {
            Id = product.Id,
            Code = product.Code,
            Name = product.Name,
            Description = product.Description,
            PurchasePrice = product.PurchasePrice,
            SalePrice = product.SalePrice,
            Stock = product.Stock, // Original stock for reference
            CurrentStock = currentStock, // Real-time stock from ProductStocks
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

    private async Task<int> GetFilteredProductCountAsync(string search = "", string categoryId = "", bool? lowStock = null, string status = "")
    {
        // Get all products matching the search and category filters
        var (allProducts, _) = await _productRepository.GetPaginatedAsync(1, int.MaxValue, search, categoryId);

        // Convert to DTOs to apply the same filtering logic
        var productsDto = new List<ProductDto>();
        foreach (var product in allProducts)
        {
            var dto = await MapToDtoAsync(product);
            productsDto.Add(dto);
        }

        // Apply the same filters as in GetPaginatedAsync
        var filteredProducts = productsDto.AsEnumerable();

        if (lowStock == true)
        {
            var globalMinimumStock = await _configurationService.GetGlobalMinimumStockAsync();
            filteredProducts = filteredProducts.Where(p =>
            {
                if (!p.Active) return false;
                var effectiveMinimumStock = p.MinimumStock > 0 ? p.MinimumStock : globalMinimumStock;
                return p.CurrentStock <= effectiveMinimumStock;
            });
        }

        if (!string.IsNullOrEmpty(status))
        {
            if (status == "active")
                filteredProducts = filteredProducts.Where(p => p.Active);
            else if (status == "inactive")
                filteredProducts = filteredProducts.Where(p => !p.Active);
        }

        return filteredProducts.Count();
    }
}
