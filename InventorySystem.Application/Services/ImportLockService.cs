using InventorySystem.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace InventorySystem.Application.Services;

public interface IImportLockService
{
    Task<bool> IsImportAllowedAsync(string importType, string? storeCode = null);
    Task<bool> HasActiveJobsAsync(string importType, string? storeCode = null);
    Task<List<string>> GetActiveJobsAsync(string importType, string? storeCode = null);
    Task<bool> CanDeleteAsync(string entityType, int entityId, string? storeCode = null);
    Task<string?> GetBlockingJobMessageAsync(string importType, string? storeCode = null);
    Task<bool> HasAnyActiveJobsAsync();
    Task<List<Core.Entities.BackgroundJob>> GetAllActiveJobsAsync();
    Task CleanupStaleJobsAsync();
}

public class ImportLockService : IImportLockService
{
    private readonly IBackgroundJobRepository _backgroundJobRepository;
    private readonly ILogger<ImportLockService> _logger;

    // Estados que consideramos "activos" y que bloquean otras operaciones
    private readonly string[] _activeStatuses = { "PENDING", "PROCESSING", "QUEUED" };

    public ImportLockService(
        IBackgroundJobRepository backgroundJobRepository,
        ILogger<ImportLockService> logger)
    {
        _backgroundJobRepository = backgroundJobRepository;
        _logger = logger;
    }

    /// <summary>
    /// Verifica si se permite iniciar una nueva importación del tipo especificado
    /// BLOQUEO MUTUO: Solo un tipo de proceso puede ejecutarse a la vez
    /// </summary>
    public async Task<bool> IsImportAllowedAsync(string importType, string? storeCode = null)
    {
        try
        {
            // NUEVA LÓGICA: Verificar si hay CUALQUIER trabajo activo de CUALQUIER tipo
            var anyActiveJobs = await HasAnyActiveJobsAsync();
            
            _logger.LogInformation($"IsImportAllowedAsync: importType={importType}, storeCode={storeCode}, anyActiveJobs={anyActiveJobs}");
            
            // Si hay cualquier trabajo activo, no permitir ningún nuevo trabajo
            if (anyActiveJobs)
            {
                var activeJobs = await GetAllActiveJobsAsync();
                foreach (var job in activeJobs)
                {
                    _logger.LogInformation($"Blocking job found: JobId={job.JobId}, JobType={job.JobType}, Status={job.Status}, StoreCode={job.StoreCode}");
                }
                return false;
            }
            
            return true; // Solo permitir si NO hay trabajos activos
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error checking if import is allowed for type {importType}");
            return false; // Por seguridad, bloquear si hay error
        }
    }

    /// <summary>
    /// Verifica si hay jobs activos del tipo especificado
    /// </summary>
    public async Task<bool> HasActiveJobsAsync(string importType, string? storeCode = null)
    {
        try
        {
            var allJobs = await _backgroundJobRepository.GetAllAsync();
            _logger.LogInformation($"HasActiveJobsAsync: Found {allJobs.Count()} total jobs");
            
            var activeJobs = allJobs.Where(job => 
                job.JobType == importType &&
                _activeStatuses.Contains(job.Status) &&
                (string.IsNullOrEmpty(storeCode) || job.StoreCode == storeCode)
            ).ToList();

            _logger.LogInformation($"HasActiveJobsAsync: importType={importType}, storeCode={storeCode}, activeJobs={activeJobs.Count}");
            
            if (activeJobs.Any())
            {
                foreach (var job in activeJobs)
                {
                    _logger.LogInformation($"Active job: {job.JobId}, {job.JobType}, {job.Status}, {job.StoreCode}");
                }
            }

            return activeJobs.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error checking active jobs for type {importType}");
            return true; // Por seguridad, asumir que hay jobs activos si hay error
        }
    }

    /// <summary>
    /// Obtiene la lista de jobs activos del tipo especificado
    /// </summary>
    public async Task<List<string>> GetActiveJobsAsync(string importType, string? storeCode = null)
    {
        try
        {
            var allJobs = await _backgroundJobRepository.GetAllAsync();
            
            return allJobs.Where(job => 
                job.JobType == importType &&
                _activeStatuses.Contains(job.Status) &&
                (string.IsNullOrEmpty(storeCode) || job.StoreCode == storeCode)
            ).Select(job => job.JobId).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting active jobs for type {importType}");
            return new List<string>();
        }
    }

    /// <summary>
    /// Verifica si se puede eliminar una entidad (producto, cliente, etc.)
    /// </summary>
    public async Task<bool> CanDeleteAsync(string entityType, int entityId, string? storeCode = null)
    {
        try
        {
            switch (entityType.ToUpper())
            {
                case "PRODUCT":
                    // No permitir eliminar productos si hay cargas activas de ventas o stock
                    return !await HasActiveJobsAsync("SALES_IMPORT") && 
                           !await HasActiveJobsAsync("STOCK_IMPORT") &&
                           !await HasActiveJobsAsync("PRODUCTS_IMPORT");

                case "CUSTOMER":
                    // No permitir eliminar clientes si hay cargas de ventas activas
                    return !await HasActiveJobsAsync("SALES_IMPORT");

                case "STORE":
                    // No permitir eliminar tiendas si hay cargas activas relacionadas
                    return !await HasActiveJobsAsync("SALES_IMPORT", storeCode) && 
                           !await HasActiveJobsAsync("STOCK_IMPORT", storeCode);

                case "SALE":
                    // No permitir eliminar ventas si hay cargas activas de ventas
                    return !await HasActiveJobsAsync("SALES_IMPORT", storeCode);

                default:
                    return true; // Por defecto permitir eliminación
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error checking if {entityType} {entityId} can be deleted");
            return false; // Por seguridad, no permitir eliminación si hay error
        }
    }

    /// <summary>
    /// Verifica si hay CUALQUIER trabajo activo de CUALQUIER tipo (para bloqueo mutuo)
    /// </summary>
    public async Task<bool> HasAnyActiveJobsAsync()
    {
        try
        {
            // Primero limpiar jobs obsoletos
            await CleanupStaleJobsAsync();
            
            var allJobs = await _backgroundJobRepository.GetAllAsync();
            var activeJobs = allJobs.Where(job => _activeStatuses.Contains(job.Status)).ToList();
            
            _logger.LogInformation($"HasAnyActiveJobsAsync: Found {activeJobs.Count} active jobs out of {allJobs.Count()} total jobs");
            
            return activeJobs.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for any active jobs");
            return true; // Por seguridad, asumir que hay jobs activos si hay error
        }
    }

    /// <summary>
    /// Obtiene todos los trabajos activos (para logging y diagnóstico)
    /// </summary>
    public async Task<List<Core.Entities.BackgroundJob>> GetAllActiveJobsAsync()
    {
        try
        {
            var allJobs = await _backgroundJobRepository.GetAllAsync();
            return allJobs.Where(job => _activeStatuses.Contains(job.Status)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all active jobs");
            return new List<Core.Entities.BackgroundJob>();
        }
    }

    /// <summary>
    /// Obtiene un mensaje descriptivo del job que está bloqueando la operación
    /// BLOQUEO MUTUO: Cualquier trabajo activo bloquea cualquier nuevo trabajo
    /// </summary>
    public async Task<string?> GetBlockingJobMessageAsync(string importType, string? storeCode = null)
    {
        try
        {
            // Nueva lógica: obtener CUALQUIER trabajo activo
            var activeJobs = await GetAllActiveJobsAsync();

            if (!activeJobs.Any())
                return null;

            var firstJob = activeJobs.OrderBy(j => j.StartedAt).First();
            var jobTypeDisplay = firstJob.JobType switch
            {
                "SALES_IMPORT" => "carga de ventas",
                "PRODUCTS_IMPORT" => "carga de productos", 
                "STOCK_IMPORT" => "carga de stock inicial",
                _ => "carga"
            };

            var storeInfo = !string.IsNullOrEmpty(firstJob.StoreCode) ? $" en tienda {firstJob.StoreCode}" : "";
            var progress = "";

            var requestedTypeDisplay = importType switch
            {
                "SALES_IMPORT" => "carga de ventas",
                "PRODUCTS_IMPORT" => "carga de productos",
                "STOCK_IMPORT" => "carga de stock inicial",
                _ => "carga"
            };

            return $"No se puede iniciar la {requestedTypeDisplay} porque hay una {jobTypeDisplay}{storeInfo} en proceso{progress}. " +
                   $"Job ID: {firstJob.JobId}. Solo se permite un proceso a la vez. Debe esperar a que termine antes de iniciar otra carga.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting blocking job message for {importType}");
            return "Hay cargas en proceso. Solo se permite un proceso a la vez. Intente nuevamente más tarde.";
        }
    }

    /// <summary>
    /// Limpia jobs que están en estado "activo" pero son demasiado antiguos (probablemente colgados)
    /// </summary>
    public async Task CleanupStaleJobsAsync()
    {
        try
        {
            var allJobs = await _backgroundJobRepository.GetAllAsync();
            var staleJobs = allJobs.Where(job => 
                _activeStatuses.Contains(job.Status) && 
                job.StartedAt < DateTime.UtcNow.AddHours(-2) // Jobs más antiguos de 2 horas
            ).ToList();

            foreach (var staleJob in staleJobs)
            {
                _logger.LogWarning($"Cleaning up stale job: {staleJob.JobId}, Type: {staleJob.JobType}, Status: {staleJob.Status}, Started: {staleJob.StartedAt}");
                
                await _backgroundJobRepository.UpdateStatusAsync(staleJob.JobId, "FAILED", 
                    "Job marcado como fallido por estar colgado más de 2 horas");
            }

            if (staleJobs.Any())
            {
                _logger.LogInformation($"Cleaned up {staleJobs.Count} stale jobs");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up stale jobs");
        }
    }
}