using Microsoft.EntityFrameworkCore;
using InventorySystem.Core.Entities;
using InventorySystem.Core.Interfaces;
using InventorySystem.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace InventorySystem.Infrastructure.Repositories;

public class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(InventoryDbContext context, ILogger<CategoryRepository> logger) : base(context, logger)
    {
    }

    public async Task<IEnumerable<Category>> GetActiveCategoriesAsync()
    {
        try
        {
            _logger.LogDebug("Getting active categories");
            var categories = await _dbSet
                .Where(c => c.Active)
                .OrderBy(c => c.Name)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} active categories", categories.Count);
            return categories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active categories");
            throw;
        }
    }

    public async Task<bool> HasProductsAsync(int categoryId)
    {
        try
        {
            _logger.LogDebug("Checking if category {CategoryId} has products", categoryId);
            var hasProducts = await _context.Products
                .AnyAsync(p => p.CategoryId == categoryId && !p.IsDeleted);

            _logger.LogDebug("Category {CategoryId} has products: {HasProducts}", categoryId, hasProducts);
            return hasProducts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if category {CategoryId} has products", categoryId);
            throw;
        }
    }

    public async Task<Category?> GetByNameAsync(string name)
    {
        try
        {
            _logger.LogDebug("Getting category by name: {Name}", name);
            var category = await _dbSet
                .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());

            if (category == null)
            {
                _logger.LogDebug("Category with name {Name} not found", name);
            }

            return category;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category by name: {Name}", name);
            throw;
        }
    }
}
