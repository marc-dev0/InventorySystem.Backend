using Microsoft.EntityFrameworkCore;
using InventorySystem.Core.Entities;
using InventorySystem.Core.Interfaces;
using InventorySystem.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace InventorySystem.Infrastructure.Repositories;

public class ProductRepository : Repository<Product>, IProductRepository
{
    private readonly IProductStockRepository _productStockRepository;

    public ProductRepository(InventoryDbContext context, ILogger<ProductRepository> logger, IProductStockRepository productStockRepository) : base(context, logger)
    {
        _productStockRepository = productStockRepository;
    }

    public override async Task<Product?> GetByIdAsync(int id)
    {
        try
        {
            _logger.LogDebug("Getting product by ID with related data: {Id}", id);
            var product = await _dbSet
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                _logger.LogWarning("Product with ID {Id} not found", id);
            }
            else
            {
                _logger.LogDebug("Found product: {ProductName} (Code: {ProductCode})", product.Name, product.Code);
            }

            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product by ID: {Id}", id);
            throw;
        }
    }

    public override async Task<IEnumerable<Product>> GetAllAsync()
    {
        try
        {
            _logger.LogDebug("Getting all products (including inactive) with related data");
            var products = await _dbSet
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .OrderBy(p => p.Name)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} total products", products.Count);
            return products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all products");
            throw;
        }
    }

    public async Task<(IEnumerable<Product> Products, int TotalCount)> GetPaginatedAsync(int page, int pageSize, string search = "", string categoryId = "")
    {
        try
        {
            _logger.LogDebug("Getting paginated products: page={Page}, pageSize={PageSize}, search={Search}, categoryId={CategoryId}", 
                page, pageSize, search, categoryId);

            var query = _dbSet
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .AsQueryable();

            // Apply search filter (case-insensitive using PostgreSQL ILIKE)
            if (!string.IsNullOrEmpty(search))
            {
                _logger.LogDebug("Applying search filter with term: '{SearchTerm}'", search);
                var searchPattern = $"%{search}%";
                query = query.Where(p =>
                    EF.Functions.ILike(p.Code, searchPattern) ||
                    EF.Functions.ILike(p.Name, searchPattern) ||
                    (p.Category != null && EF.Functions.ILike(p.Category.Name, searchPattern)) ||
                    (p.Supplier != null && EF.Functions.ILike(p.Supplier.Name, searchPattern)));
                _logger.LogDebug("Search pattern applied: '{SearchPattern}'", searchPattern);
            }

            // Apply category filter
            if (!string.IsNullOrEmpty(categoryId) && int.TryParse(categoryId, out var catId))
            {
                query = query.Where(p => p.CategoryId == catId);
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var products = await query
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} products (page {Page} of {PageSize}) with total {TotalCount}",
                products.Count, page, pageSize, totalCount);

            // Debug: if search was provided, log some sample results
            if (!string.IsNullOrEmpty(search) && products.Any())
            {
                var sampleProducts = products.Take(3);
                _logger.LogDebug("Sample search results for '{SearchTerm}':", search);
                foreach (var product in sampleProducts)
                {
                    _logger.LogDebug("- {ProductCode}: {ProductName}", product.Code, product.Name);
                }
            }

            return (products, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paginated products");
            throw;
        }
    }

    public async Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId)
    {
        try
        {
            _logger.LogDebug("Getting products by category ID: {CategoryId}", categoryId);
            var products = await _dbSet
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Where(p => p.CategoryId == categoryId && p.Active)
                .OrderBy(p => p.Name)
                .ToListAsync();

            _logger.LogInformation("Found {Count} products in category {CategoryId}", products.Count, categoryId);
            return products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products by category: {CategoryId}", categoryId);
            throw;
        }
    }

    public async Task<IEnumerable<Product>> GetBySupplierAsync(int supplierId)
    {
        try
        {
            _logger.LogDebug("Getting products by supplier ID: {SupplierId}", supplierId);
            var products = await _dbSet
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Where(p => p.SupplierId == supplierId && p.Active)
                .OrderBy(p => p.Name)
                .ToListAsync();

            _logger.LogInformation("Found {Count} products from supplier {SupplierId}", products.Count, supplierId);
            return products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products by supplier: {SupplierId}", supplierId);
            throw;
        }
    }

    public async Task<IEnumerable<Product>> GetLowStockProductsAsync()
    {
        try
        {
            _logger.LogDebug("Getting products with low stock");
            
            // Get all ProductStocks with low stock and include the related Product
            var lowStockProductStocks = await _context.ProductStocks
                .Include(ps => ps.Product)
                    .ThenInclude(p => p.Category)
                .Include(ps => ps.Product.Supplier)
                .Where(ps => ps.CurrentStock <= ps.MinimumStock && ps.Product.Active)
                .OrderBy(ps => ps.CurrentStock)
                .ToListAsync();

            var products = lowStockProductStocks.Select(ps => ps.Product).Distinct().ToList();

            _logger.LogWarning("Found {Count} products with low stock", products.Count);
            foreach (var productStock in lowStockProductStocks)
            {
                var product = productStock.Product;
                _logger.LogWarning("Low stock alert: {ProductName} (Code: {Code}) - Current: {CurrentStock}, Minimum: {MinStock}, Store: {StoreId}", 
                    product.Name, product.Code, productStock.CurrentStock, productStock.MinimumStock, productStock.StoreId);
            }

            return products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting low stock products");
            throw;
        }
    }

    public async Task<Product?> GetByCodeAsync(string code)
    {
        try
        {
            _logger.LogDebug("Getting product by code: {Code}", code);
            var product = await _dbSet
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .FirstOrDefaultAsync(p => p.Code == code);

            if (product == null)
            {
                _logger.LogWarning("Product with code {Code} not found", code);
            }
            else
            {
                _logger.LogDebug("Found product by code: {ProductName} (ID: {Id})", product.Name, product.Id);
            }

            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product by code: {Code}", code);
            throw;
        }
    }

    public async Task<bool> CodeExistsAsync(string code, int? excludeId = null)
    {
        try
        {
            _logger.LogDebug("Checking if product code exists: {Code}, excluding ID: {ExcludeId}", code, excludeId);
            var query = _dbSet.Where(p => p.Code == code);
            if (excludeId.HasValue)
            {
                query = query.Where(p => p.Id != excludeId.Value);
            }
            
            var exists = await query.AnyAsync();
            _logger.LogDebug("Product code {Code} exists: {Exists}", code, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if product code exists: {Code}", code);
            throw;
        }
    }

    public async Task UpdateStockAsync(int productId, decimal newStock)
    {
        try
        {
            _logger.LogDebug("Updating stock for product ID {ProductId} to {NewStock}", productId, newStock);
            var product = await GetByIdAsync(productId);
            if (product != null)
            {
                // Get all ProductStocks for this product across all stores
                var productStocks = await _productStockRepository.GetByProductIdAsync(productId);
                
                if (productStocks.Any())
                {
                    // Update the first store's stock (or you could update all stores proportionally)
                    var primaryProductStock = productStocks.First();
                    var oldStock = primaryProductStock.CurrentStock;
                    primaryProductStock.CurrentStock = newStock;
                    primaryProductStock.UpdatedAt = DateTime.UtcNow;
                    
                    await _productStockRepository.UpdateAsync(primaryProductStock);
                    
                    _logger.LogInformation("Stock updated for product {ProductName} (ID: {ProductId}) in store {StoreId}: {OldStock} -> {NewStock}", 
                        product.Name, productId, primaryProductStock.StoreId, oldStock, newStock);
                }
                else
                {
                    _logger.LogWarning("No ProductStock records found for product ID {ProductId}", productId);
                }
                
                // Also update the deprecated Products.Stock field for backward compatibility
                product.Stock = newStock;
                await UpdateAsync(product);
            }
            else
            {
                _logger.LogWarning("Cannot update stock: Product with ID {ProductId} not found", productId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating stock for product ID: {ProductId}", productId);
            throw;
        }
    }

    public async Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm)
    {
        try
        {
            _logger.LogDebug("Searching products with term: {SearchTerm}", searchTerm);
            var searchPattern = $"%{searchTerm}%";
            var products = await _dbSet
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Where(p => p.Active &&
                           (EF.Functions.ILike(p.Name, searchPattern) ||
                            EF.Functions.ILike(p.Code, searchPattern) ||
                            (p.Description != null && EF.Functions.ILike(p.Description, searchPattern))))
                .OrderBy(p => p.Name)
                .ToListAsync();

            _logger.LogInformation("Found {Count} products matching search term: {SearchTerm}", products.Count, searchTerm);
            return products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products with term: {SearchTerm}", searchTerm);
            throw;
        }
    }
}
