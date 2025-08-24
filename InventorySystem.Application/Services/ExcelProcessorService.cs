using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using InventorySystem.Application.DTOs;
using InventorySystem.Application.Interfaces;

namespace InventorySystem.Application.Services;

public class ExcelProcessorService
{
    private readonly ILogger<ExcelProcessorService> _logger;
    private readonly ISystemConfigurationRepository _configRepository;

    public ExcelProcessorService(ILogger<ExcelProcessorService> logger, ISystemConfigurationRepository configRepository)
    {
        _logger = logger;
        _configRepository = configRepository;
        _logger.LogInformation("ExcelProcessorService inicializado con ClosedXML (librería .NET nativa)");
    }

    public async Task<ExcelProcessResult> ProcessProductsAsync(Stream excelStream, string fileName)
    {
        try
        {
            _logger.LogInformation("Procesando archivo {FileName} para productos con ClosedXML", fileName);
            
            var startTime = DateTime.UtcNow;
            var result = new ExcelProcessResult { Type = "PRODUCTS" };
            var data = new List<Dictionary<string, object>>();
            var errors = new List<string>();
            var warnings = new List<string>();

            using var workbook = new XLWorkbook(excelStream);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            
            if (worksheet == null)
            {
                errors.Add("No se encontraron hojas en el archivo Excel");
                result.Errors = errors;
                result.ErrorCount = 1;
                return result;
            }

            // Get expected columns from generic configuration
            var expectedColumns = await GetExpectedColumnsAsync("PRODUCTS");
            
            var headerRow = worksheet.Row(1);
            var actualColumns = new List<string>();
            
            var lastColumn = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
            for (int col = 1; col <= lastColumn; col++)
            {
                var cellValue = headerRow.Cell(col).GetString().Trim();
                actualColumns.Add(cellValue);
            }
            
            // Check if columns match exactly
            if (actualColumns.Count != expectedColumns.Count)
            {
                errors.Add($"Error en formato del archivo: Se esperaban {expectedColumns.Count} columnas, pero se encontraron {actualColumns.Count}");
                errors.Add($"Columnas esperadas: {string.Join(", ", expectedColumns)}");
                errors.Add($"Columnas encontradas: {string.Join(", ", actualColumns)}");
                result.Errors = errors;
                result.ErrorCount = 1;
                return result;
            }
            
            // Check if column names match exactly
            for (int i = 0; i < expectedColumns.Count; i++)
            {
                if (actualColumns[i] != expectedColumns[i])
                {
                    errors.Add($"Error en columna {i + 1}: Se esperaba '{expectedColumns[i]}' pero se encontró '{actualColumns[i]}'");
                }
            }
            
            if (errors.Any())
            {
                errors.Insert(0, "El archivo Excel no tiene el formato correcto:");
                errors.Add($"Formato esperado: {string.Join(", ", expectedColumns)}");
                result.Errors = errors;
                result.ErrorCount = errors.Count;
                return result;
            }
            
            _logger.LogInformation("Validación de columnas exitosa para archivo {FileName}", fileName);

            var rows = worksheet.RowsUsed().Skip(1); // Skip header
            var totalRecords = rows.Count();
            result.TotalRecords = totalRecords;

            int successCount = 0;
            int errorCount = 0;

            await Task.Run(() => 
            {
                foreach (var row in rows)
                {
                    try
                    {
                        var productData = new Dictionary<string, object>();
                        
                        // Mapear las columnas según el formato del Excel real que ya funciona:
                        // 1=Tienda, 2=Código, 3=Cod.barras, 4=Nombre, 5=Descripción, 6=Categorias, 7=Marca, 8=Características, 9=Impuestos, 10=P.costo, 11=Estado, 12=Stock, 13=StockMin, 14=Ubicación, 15=P.venta, 16=Unidad
                        productData["StoreName"] = row.Cell(1).GetString().Trim();
                        productData["Code"] = row.Cell(2).GetString().Trim();
                        productData["BarCode"] = row.Cell(3).GetString().Trim();
                        productData["Name"] = row.Cell(4).GetString().Trim();
                        productData["Description"] = row.Cell(5).GetString().Trim();
                        productData["CategoryName"] = row.Cell(6).GetString().Trim();
                        productData["SupplierName"] = row.Cell(7).GetString().Trim(); // Marca
                        productData["Characteristics"] = row.Cell(8).GetString().Trim();
                        productData["Taxes"] = row.Cell(9).GetString().Trim();
                        
                        // P. costo (columna 10)
                        if (decimal.TryParse(row.Cell(10).GetString(), out var purchasePrice))
                            productData["PurchasePrice"] = purchasePrice;
                        else
                            productData["PurchasePrice"] = 0m;
                        
                        // Estado (columna 11)
                        var statusValue = row.Cell(11).GetString().Trim().ToUpper();
                        productData["Active"] = statusValue == "ACTIVO" || statusValue == "ACTIVE" || statusValue == "TRUE" || statusValue == "1";
                        
                        // Stock (columna 12)
                        if (decimal.TryParse(row.Cell(12).GetString(), out var stock))
                            productData["Stock"] = stock;
                        else
                            productData["Stock"] = 0;
                            
                        // Stock mínimo (columna 13)
                        if (decimal.TryParse(row.Cell(13).GetString(), out var minStock))
                            productData["MinimumStock"] = minStock;
                        else
                            productData["MinimumStock"] = 0;
                            
                        productData["Location"] = row.Cell(14).GetString().Trim();
                        
                        // P. venta (columna 15)
                        if (decimal.TryParse(row.Cell(15).GetString(), out var salePrice))
                            productData["SalePrice"] = salePrice;
                        else
                            productData["SalePrice"] = 0m;
                            
                        productData["Unit"] = row.Cell(16).GetString().Trim();

                        // Validaciones básicas
                        if (string.IsNullOrEmpty(productData["Code"]?.ToString()))
                        {
                            errors.Add($"Fila {row.RowNumber()}: Código de producto requerido");
                            errorCount++;
                            continue;
                        }

                        if (string.IsNullOrEmpty(productData["Name"]?.ToString()))
                        {
                            errors.Add($"Fila {row.RowNumber()}: Nombre de producto requerido");
                            errorCount++;
                            continue;
                        }

                        data.Add(productData);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Fila {row.RowNumber()}: {ex.Message}");
                        errorCount++;
                    }
                }
            });

            var processingTime = DateTime.UtcNow - startTime;
            
            result.SuccessCount = successCount;
            result.ErrorCount = errorCount;
            result.Data = data;
            result.Errors = errors;
            result.Warnings = warnings;
            result.ProcessingTime = $"{processingTime.TotalSeconds:F2} segundos";

            _logger.LogInformation("Procesamiento completado: {SuccessCount} exitosos, {ErrorCount} errores en {ProcessingTime}", 
                successCount, errorCount, result.ProcessingTime);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar productos con ClosedXML");
            return new ExcelProcessResult
            {
                Type = "PRODUCTS",
                ErrorCount = 1,
                Errors = new List<string> { $"Error general: {ex.Message}" }
            };
        }
    }

    public async Task<ExcelProcessResult> ProcessStockAsync(Stream excelStream, string fileName, string storeCode)
    {
        try
        {
            _logger.LogInformation("Procesando archivo {FileName} para stock con ClosedXML", fileName);
            
            var startTime = DateTime.UtcNow;
            var result = new ExcelProcessResult { Type = "STOCK" };
            var data = new List<Dictionary<string, object>>();
            var errors = new List<string>();
            var warnings = new List<string>();

            using var workbook = new XLWorkbook(excelStream);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            
            if (worksheet == null)
            {
                errors.Add("No se encontraron hojas en el archivo Excel");
                result.Errors = errors;
                result.ErrorCount = 1;
                return result;
            }

            var rows = worksheet.RowsUsed().Skip(1); // Skip header
            var totalRecords = rows.Count();
            result.TotalRecords = totalRecords;

            int successCount = 0;
            int errorCount = 0;

            await Task.Run(() => 
            {
                foreach (var row in rows)
                {
                    try
                    {
                        var stockData = new Dictionary<string, object>();
                        
                        stockData["ProductCode"] = row.Cell(1).GetString().Trim();
                        stockData["StoreCode"] = storeCode;
                        
                        if (int.TryParse(row.Cell(2).GetString(), out var quantity))
                            stockData["Quantity"] = quantity;
                        else
                            stockData["Quantity"] = 0;
                            
                        stockData["LastUpdated"] = DateTime.UtcNow;

                        // Validaciones básicas
                        if (string.IsNullOrEmpty(stockData["ProductCode"]?.ToString()))
                        {
                            errors.Add($"Fila {row.RowNumber()}: Código de producto requerido");
                            errorCount++;
                            continue;
                        }

                        data.Add(stockData);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Fila {row.RowNumber()}: {ex.Message}");
                        errorCount++;
                    }
                }
            });

            var processingTime = DateTime.UtcNow - startTime;
            
            result.SuccessCount = successCount;
            result.ErrorCount = errorCount;
            result.Data = data;
            result.Errors = errors;
            result.Warnings = warnings;
            result.ProcessingTime = $"{processingTime.TotalSeconds:F2} segundos";

            _logger.LogInformation("Procesamiento de stock completado: {SuccessCount} exitosos, {ErrorCount} errores en {ProcessingTime}", 
                successCount, errorCount, result.ProcessingTime);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar stock con ClosedXML");
            return new ExcelProcessResult
            {
                Type = "STOCK",
                ErrorCount = 1,
                Errors = new List<string> { $"Error general: {ex.Message}" }
            };
        }
    }

    public async Task<ExcelProcessResult> ProcessSalesAsync(Stream excelStream, string fileName, string storeCode)
    {
        try
        {
            _logger.LogInformation("Procesando archivo {FileName} para ventas con ClosedXML", fileName);
            
            var startTime = DateTime.UtcNow;
            var result = new ExcelProcessResult { Type = "SALES" };
            var data = new List<Dictionary<string, object>>();
            var errors = new List<string>();
            var warnings = new List<string>();

            using var workbook = new XLWorkbook(excelStream);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            
            if (worksheet == null)
            {
                errors.Add("No se encontraron hojas en el archivo Excel");
                result.Errors = errors;
                result.ErrorCount = 1;
                return result;
            }

            var rows = worksheet.RowsUsed().Skip(1); // Skip header
            var totalRecords = rows.Count();
            result.TotalRecords = totalRecords;

            int successCount = 0;
            int errorCount = 0;

            await Task.Run(() => 
            {
                foreach (var row in rows)
                {
                    try
                    {
                        var saleData = new Dictionary<string, object>();
                        
                        saleData["SaleNumber"] = row.Cell(1).GetString().Trim();
                        
                        if (DateTime.TryParse(row.Cell(2).GetString(), out var saleDate))
                            saleData["SaleDate"] = saleDate;
                        else
                            saleData["SaleDate"] = DateTime.UtcNow;
                            
                        if (decimal.TryParse(row.Cell(3).GetString(), out var total))
                            saleData["Total"] = total;
                        else
                            saleData["Total"] = 0m;
                            
                        saleData["CustomerDocument"] = row.Cell(4).GetString().Trim();
                        saleData["CustomerName"] = row.Cell(5).GetString().Trim();
                        saleData["StoreCode"] = storeCode;

                        // Validaciones básicas
                        if (string.IsNullOrEmpty(saleData["SaleNumber"]?.ToString()))
                        {
                            errors.Add($"Fila {row.RowNumber()}: Número de venta requerido");
                            errorCount++;
                            continue;
                        }

                        data.Add(saleData);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Fila {row.RowNumber()}: {ex.Message}");
                        errorCount++;
                    }
                }
            });

            var processingTime = DateTime.UtcNow - startTime;
            
            result.SuccessCount = successCount;
            result.ErrorCount = errorCount;
            result.Data = data;
            result.Errors = errors;
            result.Warnings = warnings;
            result.ProcessingTime = $"{processingTime.TotalSeconds:F2} segundos";

            _logger.LogInformation("Procesamiento de ventas completado: {SuccessCount} exitosos, {ErrorCount} errores en {ProcessingTime}", 
                successCount, errorCount, result.ProcessingTime);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar ventas con ClosedXML");
            return new ExcelProcessResult
            {
                Type = "SALES",
                ErrorCount = 1,
                Errors = new List<string> { $"Error general: {ex.Message}" }
            };
        }
    }

    // Métodos de compatibilidad para mantener la misma interfaz
    public Task<ExcelProcessResult> ProcessSalesFastAsync(Stream excelStream, string fileName, string storeCode)
    {
        // Simplemente redirige al método normal ya que ahora todo es nativo
        return ProcessSalesAsync(excelStream, fileName, storeCode);
    }

    public Task<bool> IsServiceHealthyAsync()
    {
        // Siempre saludable ya que es nativo
        return Task.FromResult(true);
    }

    private async Task<List<string>> GetExpectedColumnsAsync(string importType)
    {
        try
        {
            var configKey = $"IMPORT_COLUMNS_{importType}";
            var columnsJson = await _configRepository.GetConfigValueAsync(configKey);
            
            if (string.IsNullOrEmpty(columnsJson))
            {
                _logger.LogWarning("No column configuration found for import type: {ImportType}", importType);
                return new List<string>();
            }

            var columns = System.Text.Json.JsonSerializer.Deserialize<List<string>>(columnsJson);
            return columns ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving column configuration for import type: {ImportType}", importType);
            return new List<string>();
        }
    }
}

// DTOs simplificados para el procesamiento nativo
public class ExcelProcessResult
{
    public int TotalRecords { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public int SkippedCount { get; set; }
    public string ProcessingTime { get; set; } = string.Empty;
    public List<Dictionary<string, object>> Data { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public string Type { get; set; } = string.Empty;
}