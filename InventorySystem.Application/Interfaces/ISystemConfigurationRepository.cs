using InventorySystem.Core.Entities;

namespace InventorySystem.Application.Interfaces;

public interface ISystemConfigurationRepository
{
    Task<SystemConfiguration> AddAsync(SystemConfiguration entity);
    Task<SystemConfiguration?> GetByIdAsync(int id);
    Task<SystemConfiguration?> GetByKeyAsync(string configKey);
    Task<List<SystemConfiguration>> GetByCategoryAsync(string category);
    Task<List<SystemConfiguration>> GetAllAsync();
    Task UpdateAsync(SystemConfiguration entity);
    Task DeleteAsync(SystemConfiguration entity);
    Task<string?> GetConfigValueAsync(string configKey);
    Task<T?> GetConfigValueAsync<T>(string configKey) where T : class;
}