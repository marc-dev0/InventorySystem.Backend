using InventorySystem.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace InventorySystem.Application.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly ISystemConfigurationRepository _configurationRepository;
        private readonly ILogger<ConfigurationService> _logger;

        public ConfigurationService(
            ISystemConfigurationRepository configurationRepository,
            ILogger<ConfigurationService> logger)
        {
            _configurationRepository = configurationRepository;
            _logger = logger;
        }

        /// <summary>
        /// Gets the global minimum stock value from system configuration
        /// </summary>
        /// <returns>Global minimum stock value, defaults to 5 if not configured</returns>
        public async Task<decimal> GetGlobalMinimumStockAsync()
        {
            try
            {
                var configValue = await _configurationRepository.GetConfigValueAsync("GLOBAL_MINIMUM_STOCK");

                if (string.IsNullOrEmpty(configValue))
                {
                    _logger.LogWarning("GLOBAL_MINIMUM_STOCK configuration not found, using default value of 5");
                    return 5m; // Default value
                }

                if (decimal.TryParse(configValue, out var minimumStock))
                {
                    _logger.LogDebug("Global minimum stock retrieved: {MinimumStock}", minimumStock);
                    return minimumStock;
                }

                _logger.LogWarning("Invalid GLOBAL_MINIMUM_STOCK configuration value: {ConfigValue}, using default value of 5", configValue);
                return 5m; // Default value if parse fails
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving global minimum stock configuration, using default value of 5");
                return 5m; // Default value on error
            }
        }

        /// <summary>
        /// Determines if a product has low stock based on global minimum stock configuration
        /// </summary>
        /// <param name="currentStock">Current stock of the product</param>
        /// <param name="productMinimumStock">Product-specific minimum stock (if set)</param>
        /// <returns>True if the product has low stock</returns>
        public async Task<bool> IsLowStockAsync(decimal currentStock, decimal productMinimumStock)
        {
            // If product has specific minimum stock set, use that
            if (productMinimumStock > 0)
            {
                return currentStock <= productMinimumStock;
            }

            // Otherwise, use global minimum stock
            var globalMinimumStock = await GetGlobalMinimumStockAsync();
            return currentStock <= globalMinimumStock;
        }
    }
}