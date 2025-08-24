using Microsoft.EntityFrameworkCore;
using InventorySystem.Core.Entities;
using InventorySystem.Core.Interfaces;
using InventorySystem.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace InventorySystem.Infrastructure.Repositories;

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(InventoryDbContext context, ILogger<ProductRepository> logger) : base(context, logger)
    {
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
            var products = await _dbSet
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Where(p => p.Stock <= p.MinimumStock && p.Active)
                .OrderBy(p => p.Stock)
                .ToListAsync();

            _logger.LogWarning("Found {Count} products with low stock", products.Count);
            foreach (var product in products)
            {
                _logger.LogWarning("Low stock alert: {ProductName} (Code: {Code}) - Current: {CurrentStock}, Minimum: {MinStock}", 
                    product.Name, product.Code, product.Stock, product.MinimumStock);
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
                var oldStock = product.Stock;
                product.Stock = newStock;
                await UpdateAsync(product);
                _logger.LogInformation("Stock updated for product {ProductName} (ID: {ProductId}): {OldStock} -> {NewStock}", 
                    product.Name, productId, oldStock, newStock);
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
            var products = await _dbSet
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Where(p => p.Active && 
                           (p.Name.Contains(searchTerm) || 
                            p.Code.Contains(searchTerm) || 
                            (p.Description != null && p.Description.Contains(searchTerm))))
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
