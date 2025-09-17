using InventorySystem.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace InventorySystem.Application.Services;

public interface IStockInitialValidationService
{
    Task<bool> HasCompletedProductImportsAsync();
    Task<bool> CanPerformStockInitialForStoreAsync(string storeCode);
    Task<string?> GetValidationMessageAsync(string storeCode);
}

public class StockInitialValidationService : IStockInitialValidationService
{
    private readonly IBackgroundJobRepository _backgroundJobRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly ILogger<StockInitialValidationService> _logger;

    public StockInitialValidationService(
        IBackgroundJobRepository backgroundJobRepository,
        IStoreRepository storeRepository,
        ILogger<StockInitialValidationService> logger)
    {
        _backgroundJobRepository = backgroundJobRepository;
        _storeRepository = storeRepository;
        _logger = logger;
    }

    /// <summary>
    /// Verifica si existe al menos una importación de productos completada
    /// </summary>
    public async Task<bool> HasCompletedProductImportsAsync()
    {
        try
        {
            var allJobs = await _backgroundJobRepository.GetAllAsync();
            var hasCompletedProducts = allJobs.Any(job =>
                job.JobType == "PRODUCTS_IMPORT" &&
                (job.Status == "COMPLETED" || job.Status == "COMPLETED_WITH_WARNINGS"));

            _logger.LogInformation($"HasCompletedProductImportsAsync: {hasCompletedProducts}");
            return hasCompletedProducts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for completed product imports");
            return false;
        }
    }

    /// <summary>
    /// Verifica si se puede realizar carga inicial de stock para una tienda específica
    /// </summary>
    public async Task<bool> CanPerformStockInitialForStoreAsync(string storeCode)
    {
        try
        {
            // Regla 1: Debe haber al menos una carga de productos completada
            var hasProductImports = await HasCompletedProductImportsAsync();
            if (!hasProductImports)
            {
                _logger.LogInformation($"Cannot perform stock initial for store {storeCode}: No completed product imports");
                return false;
            }

            // Regla 2: La tienda no debe tener stock inicial ya cargado
            var store = await _storeRepository.GetByCodeAsync(storeCode);
            if (store == null)
            {
                _logger.LogWarning($"Store with code {storeCode} not found");
                return false;
            }

            if (store.HasInitialStock == true)
            {
                _logger.LogInformation($"Cannot perform stock initial for store {storeCode}: Already has initial stock");
                return false;
            }

            // Regla 3: No debe haber cargas de stock COMPLETADAS para esta tienda
            var allJobs = await _backgroundJobRepository.GetAllAsync();
            var hasCompletedStockImports = allJobs.Any(job =>
                job.JobType == "STOCK_IMPORT" &&
                (job.Status == "COMPLETED" || job.Status == "COMPLETED_WITH_WARNINGS") &&
                job.StoreCode == storeCode);

            if (hasCompletedStockImports)
            {
                _logger.LogInformation($"Cannot perform stock initial for store {storeCode}: Already has completed stock imports");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error checking if stock initial can be performed for store {storeCode}");
            return false;
        }
    }

    /// <summary>
    /// Obtiene un mensaje explicativo de por qué no se puede realizar la carga
    /// </summary>
    public async Task<string?> GetValidationMessageAsync(string storeCode)
    {
        try
        {
            // Verificar si ya puede realizar la carga
            if (await CanPerformStockInitialForStoreAsync(storeCode))
            {
                return null; // No hay problemas
            }

            // Identificar el problema específico
            var hasProductImports = await HasCompletedProductImportsAsync();
            if (!hasProductImports)
            {
                return "Debe completar al menos una carga de productos antes de poder cargar stock inicial.";
            }

            var store = await _storeRepository.GetByCodeAsync(storeCode);
            if (store?.HasInitialStock == true)
            {
                return "Esta tienda ya tiene stock inicial cargado. Solo se permite una carga de stock inicial por tienda.";
            }

            // Verificar si ya tiene cargas completadas
            var allJobs = await _backgroundJobRepository.GetAllAsync();
            var hasCompletedStockImports = allJobs.Any(job =>
                job.JobType == "STOCK_IMPORT" &&
                (job.Status == "COMPLETED" || job.Status == "COMPLETED_WITH_WARNINGS") &&
                job.StoreCode == storeCode);

            if (hasCompletedStockImports)
            {
                return "Esta tienda ya tiene una carga de stock inicial completada. Solo se permite una carga de stock inicial por tienda.";
            }

            return "No se puede realizar la carga de stock inicial en este momento.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting validation message for store {storeCode}");
            return "Error al validar los requisitos para la carga de stock inicial.";
        }
    }
}