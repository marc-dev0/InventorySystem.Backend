using InventorySystem.Application.DTOs;
using InventorySystem.Application.Interfaces;
using InventorySystem.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace InventorySystem.Application.Services;

public class BatchProcessingService
{
    private readonly ILogger<BatchProcessingService> _logger;
    private readonly IBackgroundJobRepository _backgroundJobRepository;
    private const int DEFAULT_BATCH_SIZE = 500; // Procesar de 500 en 500

    public BatchProcessingService(
        ILogger<BatchProcessingService> logger,
        IBackgroundJobRepository backgroundJobRepository)
    {
        _logger = logger;
        _backgroundJobRepository = backgroundJobRepository;
    }

    /// <summary>
    /// Procesa una lista de datos en lotes para optimizar memoria y rendimiento
    /// </summary>
    public async Task<BatchProcessingResultDto> ProcessInBatchesAsync<T>(
        string jobId,
        List<T> allData,
        Func<List<T>, Task<BatchResultDto>> processFunction,
        int batchSize = DEFAULT_BATCH_SIZE)
    {
        var result = new BatchProcessingResultDto
        {
            TotalRecords = allData.Count,
            BatchSize = batchSize,
            StartTime = DateTime.UtcNow
        };

        try
        {
            var totalBatches = (int)Math.Ceiling((double)allData.Count / batchSize);
            result.TotalBatches = totalBatches;

            _logger.LogInformation($"Job {jobId}: Processing {allData.Count} records in {totalBatches} batches of {batchSize}");

            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                var currentBatch = allData
                    .Skip(batchIndex * batchSize)
                    .Take(batchSize)
                    .ToList();

                _logger.LogInformation($"Job {jobId}: Processing batch {batchIndex + 1}/{totalBatches} ({currentBatch.Count} records)");

                try
                {
                    // Procesar el lote actual
                    var batchResult = await processFunction(currentBatch);
                    
                    // Acumular resultados
                    result.ProcessedRecords += batchResult.ProcessedCount;
                    result.SuccessRecords += batchResult.SuccessCount;
                    result.ErrorRecords += batchResult.ErrorCount;
                    result.WarningRecords += batchResult.WarningCount;
                    result.BatchResults.Add(batchResult);

                    // Agregar errores del lote al job
                    foreach (var error in batchResult.Errors)
                    {
                        await _backgroundJobRepository.AddErrorAsync(jobId, $"BATCH {batchIndex + 1}: {error}");
                    }

                    // Agregar warnings del lote al job
                    foreach (var warning in batchResult.Warnings)
                    {
                        await _backgroundJobRepository.AddWarningAsync(jobId, $"BATCH {batchIndex + 1}: {warning}");
                    }

                    // Actualizar progreso
                    var progressPercentage = ((decimal)(batchIndex + 1) / totalBatches) * 100;
                    await _backgroundJobRepository.UpdateProgressAsync(jobId, result.ProcessedRecords, progressPercentage);

                    _logger.LogInformation($"Job {jobId}: Completed batch {batchIndex + 1}/{totalBatches} - Success: {batchResult.SuccessCount}, Errors: {batchResult.ErrorCount}");

                    // Pausa pequeña entre lotes para no sobrecargar la base de datos
                    if (batchIndex < totalBatches - 1) // No hacer pausa en el último lote
                    {
                        await Task.Delay(100); // 100ms entre lotes
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Job {jobId}: Error processing batch {batchIndex + 1}");
                    
                    result.ErrorRecords += currentBatch.Count;
                    result.BatchResults.Add(new BatchResultDto
                    {
                        BatchNumber = batchIndex + 1,
                        ProcessedCount = 0,
                        SuccessCount = 0,
                        ErrorCount = currentBatch.Count,
                        WarningCount = 0,
                        Errors = new List<string> { $"Error procesando lote: {ex.Message}" },
                        ProcessingTime = TimeSpan.Zero
                    });

                    await _backgroundJobRepository.AddErrorAsync(jobId, $"BATCH {batchIndex + 1}: Error crítico - {ex.Message}");

                    // Decidir si continuar o detener
                    if (result.ErrorRecords > allData.Count * 0.5) // Si más del 50% falló, detener
                    {
                        _logger.LogError($"Job {jobId}: Too many errors ({result.ErrorRecords}), stopping batch processing");
                        result.WasStopped = true;
                        break;
                    }
                }
            }

            result.EndTime = DateTime.UtcNow;
            result.TotalProcessingTime = result.EndTime.Value - result.StartTime;
            result.IsCompleted = !result.WasStopped;

            _logger.LogInformation($"Job {jobId}: Batch processing completed - Total: {result.ProcessedRecords}, Success: {result.SuccessRecords}, Errors: {result.ErrorRecords}");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Job {jobId}: Critical error in batch processing");
            result.EndTime = DateTime.UtcNow;
            result.IsCompleted = false;
            result.WasStopped = true;
            throw;
        }
    }

    /// <summary>
    /// Calcula el tamaño de lote óptimo basado en el número total de registros
    /// </summary>
    public int CalculateOptimalBatchSize(int totalRecords)
    {
        return totalRecords switch
        {
            <= 1000 => 100,        // Lotes pequeños para archivos pequeños
            <= 5000 => 250,        // Lotes medianos
            <= 10000 => 500,       // Lotes grandes
            <= 50000 => 1000,      // Lotes muy grandes
            _ => 2000               // Lotes máximos para archivos masivos
        };
    }
}

public class BatchProcessingResultDto
{
    public int TotalRecords { get; set; }
    public int ProcessedRecords { get; set; }
    public int SuccessRecords { get; set; }
    public int ErrorRecords { get; set; }
    public int WarningRecords { get; set; }
    public int BatchSize { get; set; }
    public int TotalBatches { get; set; }
    public List<BatchResultDto> BatchResults { get; set; } = new();
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan TotalProcessingTime { get; set; }
    public bool IsCompleted { get; set; }
    public bool WasStopped { get; set; }
}

public class BatchResultDto
{
    public int BatchNumber { get; set; }
    public int ProcessedCount { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public TimeSpan ProcessingTime { get; set; }
}