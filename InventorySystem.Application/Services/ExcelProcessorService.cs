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
            var expectedColumns = await GetExpectedColumnsAsync("PRODUCT");
            
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

            // Get expected columns from generic configuration
            var expectedColumns = await GetExpectedColumnsAsync("STOCK");
            
            var headerRow = worksheet.Row(1);
            var actualColumns = new List<string>();
            
            var lastColumn = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
            for (int col = 1; col <= lastColumn; col++)
            {
                var cellValue = headerRow.Cell(col).GetString().Trim();
                actualColumns.Add(cellValue);
            }
            
            // Basic validation - check if we have the minimum required columns
            if (actualColumns.Count < expectedColumns.Count)
            {
                errors.Add($"Error en formato del archivo: Se esperaban al menos {expectedColumns.Count} columnas, pero se encontraron {actualColumns.Count}");
                errors.Add($"Columnas esperadas: {string.Join(", ", expectedColumns)}");
                errors.Add($"Columnas encontradas: {string.Join(", ", actualColumns)}");
                result.Errors = errors;
                result.ErrorCount = 1;
                return result;
            }
            
            _logger.LogInformation("Validación de columnas exitosa para archivo {FileName}", fileName);

            // Get column mapping from configuration
            var columnMapping = await GetStockColumnMappingAsync();

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
                        
                        var productCode = row.Cell(columnMapping.CodeColumn).GetString().Trim();
                        stockData["ProductCode"] = productCode;
                        stockData["codigo"] = productCode; // Para compatibilidad con FastImportsController
                        stockData["StoreCode"] = storeCode;
                        
                        if (decimal.TryParse(row.Cell(columnMapping.StockColumn).GetString(), out var quantity))
                        {
                            stockData["Quantity"] = quantity;
                            stockData["current_stock"] = quantity; // Para compatibilidad con FastImportsController
                        }
                        else
                        {
                            stockData["Quantity"] = 0;
                            stockData["current_stock"] = 0;
                        }

                        if (decimal.TryParse(row.Cell(columnMapping.MinStockColumn).GetString(), out var minQuantity))
                        {
                            stockData["MinQuantity"] = minQuantity;
                            stockData["minimum_stock"] = minQuantity; // Para compatibilidad con FastImportsController
                            stockData["maximum_stock"] = minQuantity * 3; // Calcular máximo automáticamente
                        }
                        else
                        {
                            stockData["MinQuantity"] = 0;
                            stockData["minimum_stock"] = 0;
                            stockData["maximum_stock"] = 0;
                        }
                            
                        stockData["LastUpdated"] = DateTime.UtcNow;

                        // Validaciones básicas
                        if (string.IsNullOrEmpty(productCode))
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

            // Validar columnas del Excel contra configuración
            var columnValidationResult = await ValidateSalesColumnsAsync(worksheet);
            if (!columnValidationResult.IsValid)
            {
                errors.AddRange(columnValidationResult.Errors);
                result.Errors = errors;
                result.ErrorCount = columnValidationResult.Errors.Count;
                return result;
            }

            // Obtener mapeo de columnas configurado
            var columnMapping = await GetSalesColumnMappingAsync();

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
                        
                        // Usar mapeo de columnas configurado
                        saleData["RazonSocial"] = row.Cell(columnMapping.RazonSocialColumn).GetString().Trim();
                        saleData["EmployeeName"] = row.Cell(columnMapping.EmpleadoVentaColumn).GetString().Trim();
                        saleData["Almacen"] = row.Cell(columnMapping.AlmacenColumn).GetString().Trim();
                        saleData["CustomerName"] = row.Cell(columnMapping.ClienteNombreColumn).GetString().Trim();
                        saleData["CustomerDoc"] = row.Cell(columnMapping.ClienteDocColumn).GetString().Trim();
                        var documentNumber = row.Cell(columnMapping.NumDocColumn).GetString().Trim();
                        var docRelacionado = row.Cell(columnMapping.DocRelacionadoColumn).GetString().Trim();
                        
                        // Usar el número de documento principal para agrupación
                        saleData["DocumentNumber"] = documentNumber;
                        saleData["SaleNumber"] = !string.IsNullOrEmpty(documentNumber) ? documentNumber : docRelacionado;
                        
                        if (DateTime.TryParse(row.Cell(columnMapping.FechaColumn).GetString(), out var saleDate))
                            saleData["SaleDate"] = saleDate;
                        else
                            saleData["SaleDate"] = DateTime.UtcNow;
                            
                        saleData["Hora"] = row.Cell(columnMapping.HoraColumn).GetString().Trim();
                        saleData["TipoDoc"] = row.Cell(columnMapping.TipDocColumn).GetString().Trim();
                        saleData["Unidad"] = row.Cell(columnMapping.UnidadColumn).GetString().Trim();
                        
                        if (decimal.TryParse(row.Cell(columnMapping.CantidadColumn).GetString(), out var quantity))
                            saleData["Quantity"] = quantity;
                        else
                            saleData["Quantity"] = 1m;
                            
                        if (decimal.TryParse(row.Cell(columnMapping.PrecioVentaColumn).GetString(), out var unitPrice))
                            saleData["UnitPrice"] = unitPrice;
                        else
                            saleData["UnitPrice"] = 0m;
                            
                        if (decimal.TryParse(row.Cell(columnMapping.IgvColumn).GetString(), out var igv))
                            saleData["IGV"] = igv;
                        else
                            saleData["IGV"] = 0m;
                            
                        if (decimal.TryParse(row.Cell(columnMapping.TotalColumn).GetString(), out var total))
                            saleData["Total"] = total;
                        else
                            saleData["Total"] = quantity * unitPrice;
                            
                        saleData["Subtotal"] = saleData["Total"];
                        
                        if (decimal.TryParse(row.Cell(columnMapping.DescuentoColumn).GetString(), out var descuento))
                            saleData["Descuento"] = descuento;
                        else
                            saleData["Descuento"] = 0m;
                            
                        saleData["Conversion"] = row.Cell(columnMapping.ConversionColumn).GetString().Trim();
                        saleData["Moneda"] = row.Cell(columnMapping.MonedaColumn).GetString().Trim();
                        saleData["CodigoSKU"] = row.Cell(columnMapping.CodigoSkuColumn).GetString().Trim();
                        saleData["CodAlternativo"] = row.Cell(columnMapping.CodAlternativoColumn).GetString().Trim();
                        saleData["Marca"] = row.Cell(columnMapping.MarcaColumn).GetString().Trim();
                        saleData["Categoria"] = row.Cell(columnMapping.CategoriaColumn).GetString().Trim();
                        saleData["Caracteristicas"] = row.Cell(columnMapping.CaracteristicasColumn).GetString().Trim();
                        saleData["ProductName"] = row.Cell(columnMapping.NombreColumn).GetString().Trim();
                        saleData["Descripcion"] = row.Cell(columnMapping.DescripcionColumn).GetString().Trim();
                        saleData["Proveedor"] = row.Cell(columnMapping.ProveedorColumn).GetString().Trim();
                        
                        if (decimal.TryParse(row.Cell(columnMapping.PrecioCostoColumn).GetString(), out var precioCosto))
                            saleData["PrecioCosto"] = precioCosto;
                        else
                            saleData["PrecioCosto"] = 0m;
                            
                        saleData["EmpleadoRegistro"] = row.Cell(columnMapping.EmpleadoRegistroColumn).GetString().Trim();
                        saleData["StoreCode"] = storeCode;

                        // Validaciones básicas (más flexibles como productos)
                        if (string.IsNullOrEmpty(saleData["DocumentNumber"]?.ToString()) && 
                            string.IsNullOrEmpty(saleData["SaleNumber"]?.ToString()))
                        {
                            warnings.Add($"Fila {row.RowNumber()}: Sin número de documento, omitiendo...");
                            continue;
                        }
                        
                        if (string.IsNullOrEmpty(saleData["ProductName"]?.ToString()))
                        {
                            warnings.Add($"Fila {row.RowNumber()}: Sin nombre de producto, omitiendo...");
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

    private async Task<(bool IsValid, List<string> Errors)> ValidateSalesColumnsAsync(IXLWorksheet worksheet)
    {
        var errors = new List<string>();
        
        try
        {
            // Obtener columnas esperadas de la configuración
            var expectedColumns = await GetExpectedSalesColumnsAsync();
            if (expectedColumns == null || !expectedColumns.Any())
            {
                // Si no hay configuración, usar validación mínima por defecto
                return (true, errors);
            }

            // Obtener las columnas del Excel (primera fila)
            var headerRow = worksheet.Row(1);
            var actualColumns = new List<string>();
            
            for (int col = 1; col <= expectedColumns.Count; col++)
            {
                try
                {
                    var cellValue = headerRow.Cell(col).GetString().Trim();
                    actualColumns.Add(cellValue);
                }
                catch
                {
                    actualColumns.Add("");
                }
            }

            // Validar que todas las columnas esperadas estén presentes
            for (int i = 0; i < expectedColumns.Count; i++)
            {
                if (i >= actualColumns.Count || string.IsNullOrEmpty(actualColumns[i]))
                {
                    errors.Add($"Columna {i + 1} ({expectedColumns[i]}) no encontrada o vacía");
                }
                else if (actualColumns[i] != expectedColumns[i])
                {
                    errors.Add($"Columna {i + 1}: esperada '{expectedColumns[i]}', encontrada '{actualColumns[i]}'");
                }
            }

            return (errors.Count == 0, errors);
        }
        catch (Exception ex)
        {
            errors.Add($"Error validando columnas: {ex.Message}");
            return (false, errors);
        }
    }

    private async Task<List<string>> GetExpectedSalesColumnsAsync()
    {
        try
        {
            var configKey = "IMPORT_COLUMNS_SALES";
            var config = await _configRepository.GetByKeyAsync(configKey);
            
            if (config == null || string.IsNullOrEmpty(config.ConfigValue))
            {
                // Configuración por defecto
                return new List<string>
                {
                    "Razón Social", "Empleado Venta", "Almacén", "Cliente Nombre", "Cliente Doc.",
                    "#-DOC", "# Doc. Relacionado", "Fecha", "Hora", "Tip. Doc.", "Unidad",
                    "Cantidad", "Precio de Venta", "IGV", "Total", "Descuento aplicado (%)",
                    "Conversión", "Moneda", "Codigo SKU", "Cod. alternativo", "Marca",
                    "Categoría", "Características", "Nombre", "Descripción", "Proveedor",
                    "Precio de costo", "Empleado registro"
                };
            }

            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(config.ConfigValue) ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Error obteniendo configuración de columnas de ventas: {Error}", ex.Message);
            return new List<string>();
        }
    }

    private async Task<SalesColumnMapping> GetSalesColumnMappingAsync()
    {
        try
        {
            var configKey = "IMPORT_MAPPING_SALES";
            var config = await _configRepository.GetByKeyAsync(configKey);
            
            if (config == null || string.IsNullOrEmpty(config.ConfigValue))
            {
                // Configuración por defecto
                return new SalesColumnMapping();
            }

            return System.Text.Json.JsonSerializer.Deserialize<SalesColumnMapping>(config.ConfigValue) ?? new SalesColumnMapping();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Error obteniendo mapeo de columnas de ventas: {Error}", ex.Message);
            return new SalesColumnMapping();
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

    private async Task<StockColumnMapping> GetStockColumnMappingAsync()
    {
        try
        {
            var configKey = "IMPORT_MAPPING_STOCK";
            var mappingJson = await _configRepository.GetConfigValueAsync(configKey);
            
            if (string.IsNullOrEmpty(mappingJson))
            {
                _logger.LogWarning("No stock column mapping configuration found, using defaults");
                return new StockColumnMapping();
            }

            var mapping = System.Text.Json.JsonSerializer.Deserialize<StockColumnMapping>(mappingJson);
            return mapping ?? new StockColumnMapping();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving stock column mapping configuration, using defaults");
            return new StockColumnMapping();
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