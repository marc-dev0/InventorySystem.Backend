using System.Text.Json;
using InventorySystem.Application.Interfaces;
using InventorySystem.Core.Entities;
using InventorySystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventorySystem.Infrastructure.Repositories;

public class SystemConfigurationRepository : ISystemConfigurationRepository
{
    private readonly InventoryDbContext _context;
    
    public SystemConfigurationRepository(InventoryDbContext context)
    {
        _context = context;
    }
    
    public async Task<SystemConfiguration> AddAsync(SystemConfiguration entity)
    {
        _context.SystemConfigurations.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }
    
    public async Task<SystemConfiguration?> GetByIdAsync(int id)
    {
        return await _context.SystemConfigurations.FindAsync(id);
    }
    
    public async Task<SystemConfiguration?> GetByKeyAsync(string configKey)
    {
        return await _context.SystemConfigurations
            .FirstOrDefaultAsync(c => c.ConfigKey == configKey && c.Active && !c.IsDeleted);
    }
    
    public async Task<List<SystemConfiguration>> GetByCategoryAsync(string category)
    {
        return await _context.SystemConfigurations
            .Where(c => c.Category == category && c.Active && !c.IsDeleted)
            .OrderBy(c => c.ConfigKey)
            .ToListAsync();
    }
    
    public async Task<List<SystemConfiguration>> GetAllAsync()
    {
        return await _context.SystemConfigurations
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Category)
            .ThenBy(c => c.ConfigKey)
            .ToListAsync();
    }
    
    public async Task UpdateAsync(SystemConfiguration entity)
    {
        _context.SystemConfigurations.Update(entity);
        await _context.SaveChangesAsync();
    }
    
    public async Task DeleteAsync(SystemConfiguration entity)
    {
        _context.SystemConfigurations.Remove(entity);
        await _context.SaveChangesAsync();
    }
    
    public async Task<string?> GetConfigValueAsync(string configKey)
    {
        var config = await GetByKeyAsync(configKey);
        return config?.ConfigValue;
    }
    
    public async Task<T?> GetConfigValueAsync<T>(string configKey) where T : class
    {
        var config = await GetByKeyAsync(configKey);
        if (config == null) return null;
        
        try
        {
            return config.ConfigType switch
            {
                "Json" => JsonSerializer.Deserialize<T>(config.ConfigValue),
                "String" => config.ConfigValue as T,
                _ => config.ConfigValue as T
            };
        }
        catch
        {
            return null;
        }
    }
}