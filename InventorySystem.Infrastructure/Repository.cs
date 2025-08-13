using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using InventorySystem.Core.Interfaces;
using InventorySystem.Infrastructure.Data;

namespace InventorySystem.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly InventoryDbContext _context;
    protected readonly DbSet<T> _dbSet;
    protected readonly ILogger<Repository<T>> _logger;

    public Repository(InventoryDbContext context, ILogger<Repository<T>> logger)
    {
        _context = context;
        _dbSet = context.Set<T>();
        _logger = logger;
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        try
        {
            _logger.LogDebug("Getting entity {EntityType} by ID: {Id}", typeof(T).Name, id);
            var entity = await _dbSet.FindAsync(id);
            
            if (entity == null)
            {
                _logger.LogWarning("Entity {EntityType} with ID {Id} not found", typeof(T).Name, id);
            }
            
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entity {EntityType} by ID: {Id}", typeof(T).Name, id);
            throw;
        }
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        try
        {
            _logger.LogDebug("Getting all entities of type {EntityType}", typeof(T).Name);
            var entities = await _dbSet.ToListAsync();
            _logger.LogInformation("Retrieved {Count} entities of type {EntityType}", entities.Count, typeof(T).Name);
            return entities;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all entities of type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        try
        {
            _logger.LogDebug("Finding entities of type {EntityType} with predicate", typeof(T).Name);
            var entities = await _dbSet.Where(predicate).ToListAsync();
            _logger.LogInformation("Found {Count} entities of type {EntityType} matching criteria", entities.Count, typeof(T).Name);
            return entities;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding entities of type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
    {
        try
        {
            _logger.LogDebug("Getting first entity of type {EntityType} with predicate", typeof(T).Name);
            return await _dbSet.FirstOrDefaultAsync(predicate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting first entity of type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        try
        {
            _logger.LogDebug("Adding new entity of type {EntityType}", typeof(T).Name);
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully added entity of type {EntityType}", typeof(T).Name);
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding entity of type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
    {
        try
        {
            var entityList = entities.ToList();
            _logger.LogDebug("Adding {Count} entities of type {EntityType}", entityList.Count, typeof(T).Name);
            await _dbSet.AddRangeAsync(entityList);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully added {Count} entities of type {EntityType}", entityList.Count, typeof(T).Name);
            return entityList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding range of entities of type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task UpdateAsync(T entity)
    {
        try
        {
            _logger.LogDebug("Updating entity of type {EntityType}", typeof(T).Name);
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully updated entity of type {EntityType}", typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating entity of type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task DeleteAsync(int id)
    {
        try
        {
            _logger.LogDebug("Deleting entity {EntityType} with ID: {Id}", typeof(T).Name, id);
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _dbSet.Remove(entity);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully deleted entity {EntityType} with ID: {Id}", typeof(T).Name, id);
            }
            else
            {
                _logger.LogWarning("Attempted to delete non-existent entity {EntityType} with ID: {Id}", typeof(T).Name, id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting entity {EntityType} with ID: {Id}", typeof(T).Name, id);
            throw;
        }
    }

    public virtual async Task<bool> ExistsAsync(int id)
    {
        try
        {
            var exists = await _dbSet.FindAsync(id) != null;
            _logger.LogDebug("Entity {EntityType} with ID {Id} exists: {Exists}", typeof(T).Name, id, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if entity {EntityType} with ID {Id} exists", typeof(T).Name, id);
            throw;
        }
    }

    public virtual async Task<int> CountAsync()
    {
        try
        {
            var count = await _dbSet.CountAsync();
            _logger.LogDebug("Total count of {EntityType}: {Count}", typeof(T).Name, count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting entities of type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
    {
        try
        {
            var count = await _dbSet.CountAsync(predicate);
            _logger.LogDebug("Count of {EntityType} matching criteria: {Count}", typeof(T).Name, count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting entities of type {EntityType} with predicate", typeof(T).Name);
            throw;
        }
    }
}
