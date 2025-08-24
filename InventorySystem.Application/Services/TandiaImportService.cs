using InventorySystem.Application.DTOs;
using InventorySystem.Application.Interfaces;
using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Entities;
using ClosedXML.Excel;
using System.Globalization;

namespace InventorySystem.Application.Services;

public class TandiaImportService : ITandiaImportService
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ISaleRepository _saleRepository;
    private readonly IInventoryMovementRepository _inventoryMovementRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IProductStockRepository _productStockRepository;
    private readonly IImportBatchRepository _importBatchRepository;

    public TandiaImportService(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        ISupplierRepository supplierRepository,
        ICustomerRepository customerRepository,
        ISaleRepository saleRepository,
        IInventoryMovementRepository inventoryMovementRepository,
        IStoreRepository storeRepository,
        IProductStockRepository productStockRepository,
        IImportBatchRepository importBatchRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _supplierRepository = supplierRepository;
        _customerRepository = customerRepository;
        _saleRepository = saleRepository;
        _inventoryMovementRepository = inventoryMovementRepository;
        _storeRepository = storeRepository;
        _productStockRepository = productStockRepository;
        _importBatchRepository = importBatchRepository;
    }

    public async Task<List<TandiaProductDto>> ValidateProductsExcelAsync(Stream excelStream)
    {
        var products = new List<TandiaProductDto>();
        
        using var workbook = new XLWorkbook(excelStream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        
        if (worksheet == null)
            throw new InvalidOperationException("No worksheet found in Excel file");

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        
        // Skip header row (row 1)
        for (int row = 2; row <= lastRow; row++)
        {
            var product = new TandiaProductDto
            {
                Store = GetCellValue(worksheet, row, 1),
                Code = GetCellValue(worksheet, row, 2),
                Barcode = GetCellValue(worksheet, row, 3),
                Name = GetCellValue(worksheet, row, 4),
                Description = GetCellValue(worksheet, row, 5),
                Categories = GetCellValue(worksheet, row, 6),
                Brand = GetCellValue(worksheet, row, 7),
                Features = GetCellValue(worksheet, row, 8),
                Taxes = GetCellValue(worksheet, row, 9),
                CostPrice = GetDecimalValue(worksheet, row, 10),
                Status = GetCellValue(worksheet, row, 11),
                Stock = GetIntValue(worksheet, row, 12),
                MinStock = GetIntValue(worksheet, row, 13),
                Location = GetCellValue(worksheet, row, 14),
                SalePrice = GetDecimalValue(worksheet, row, 15),
                Unit = GetCellValue(worksheet, row, 16),
                PriceListName = GetCellValue(worksheet, row, 17),
                ConversionFactor = GetDecimalValue(worksheet, row, 18),
                WholesalePrice = GetDecimalValue(worksheet, row, 19),
                MinQuantity = GetIntValue(worksheet, row, 20),
                MaxQuantity = GetIntValue(worksheet, row, 21)
            };
            
            if (!string.IsNullOrWhiteSpace(product.Code))
                products.Add(product);
        }
        
        return products;
    }

    public async Task<List<TandiaSaleDetailDto>> ValidateSalesExcelAsync(Stream excelStream)
    {
        var sales = new List<TandiaSaleDetailDto>();
        
        using var workbook = new XLWorkbook(excelStream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        
        if (worksheet == null)
            throw new InvalidOperationException("No worksheet found in Excel file");

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        // Skip header row (row 1)
        for (int row = 2; row <= lastRow; row++)
        {
            var sale = new TandiaSaleDetailDto
            {
                CompanyName = GetCellValue(worksheet, row, 1),
                SalesEmployee = GetCellValue(worksheet, row, 2),
                Warehouse = GetCellValue(worksheet, row, 3),
                CustomerName = GetCellValue(worksheet, row, 4),
                CustomerDocument = GetCellValue(worksheet, row, 5),
                DocumentNumber = GetCellValue(worksheet, row, 6),
                RelatedDocument = GetCellValue(worksheet, row, 7),
                Date = GetDateValue(worksheet, row, 8),
                Time = GetTimeValue(worksheet, row, 9),
                DocumentType = GetCellValue(worksheet, row, 10),
                Unit = GetCellValue(worksheet, row, 11),
                Quantity = GetDecimalValue(worksheet, row, 12),
                SalePrice = GetDecimalValue(worksheet, row, 13),
                Tax = GetDecimalValue(worksheet, row, 14),
                Total = GetDecimalValue(worksheet, row, 15),
                DiscountApplied = GetDecimalValue(worksheet, row, 16),
                Conversion = GetDecimalValue(worksheet, row, 17),
                Currency = GetCellValue(worksheet, row, 18),
                ProductCode = GetCellValue(worksheet, row, 19),
                AlternativeCode = GetCellValue(worksheet, row, 20),
                Brand = GetCellValue(worksheet, row, 21),
                Category = GetCellValue(worksheet, row, 22),
                Features = GetCellValue(worksheet, row, 23),
                ProductName = GetCellValue(worksheet, row, 24),
                Description = GetCellValue(worksheet, row, 25),
                Supplier = GetCellValue(worksheet, row, 26),
                CostPrice = GetDecimalValue(worksheet, row, 27),
                RegistrationEmployee = GetCellValue(worksheet, row, 28)
            };
            
            if (!string.IsNullOrWhiteSpace(sale.ProductCode))
                sales.Add(sale);
        }
        
        return sales;
    }

    private async Task<string> GenerateUniqueBatchCodeAsync(string type)
    {
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var prefix = $"{type}_{today}";
        
        // Buscar TODOS los batches existentes con este prefijo (incluyendo eliminados)
        var allBatches = await _importBatchRepository.GetAllAsync();
        var todayBatchCodes = allBatches
            .Where(b => b.BatchCode.StartsWith(prefix))
            .Select(b => b.BatchCode)
            .ToList();
        
        // Buscar el próximo número secuencial disponible
        int sequence = 1;
        string candidateCode;
        do
        {
            candidateCode = $"{prefix}_{sequence:D3}";
            sequence++;
        } while (todayBatchCodes.Contains(candidateCode));
        
        return candidateCode; // Ej: SALES_20250814_002 si 001 ya existe
    }

    public async Task<BulkUploadResultDto> ImportProductsFromExcelAsync(Stream excelStream, string fileName)
    {
        var startTime = DateTime.Now;
        var result = new BulkUploadResultDto();
        
        try
        {
            // Crear ImportBatch con código único
            var batchCode = await GenerateUniqueBatchCodeAsync("PRODUCTS");
            var importBatch = new ImportBatch
            {
                BatchCode = batchCode,
                BatchType = "PRODUCTS",
                FileName = fileName,
                ImportDate = DateTime.UtcNow,
                ImportedBy = "SYSTEM", // Podrías parametrizar esto
                TotalRecords = 0, // Se actualizará al final
                SuccessCount = 0,
                SkippedCount = 0,
                ErrorCount = 0
            };
            
            // Guardar batch para obtener ID
            await _importBatchRepository.AddAsync(importBatch);
            
            var tandiaProducts = await ValidateProductsExcelAsync(excelStream);
            result.TotalRecords = tandiaProducts.Count;
            
            // Actualizar total records
            importBatch.TotalRecords = tandiaProducts.Count;
            
            foreach (var tandiaProduct in tandiaProducts)
            {
                try
                {
                    // Check if product already exists
                    var existingProduct = await _productRepository.FirstOrDefaultAsync(p => p.Code == tandiaProduct.Code);
                    
                    if (existingProduct != null)
                    {
                        // Skip existing product - no update
                        result.SkippedCount++;
                        importBatch.SkippedCount++;
                        continue;
                    }
                    
                    // Get or create category
                    var category = await GetOrCreateCategoryAsync(tandiaProduct.Categories);
                    
                    // Get or create supplier
                    Supplier? supplier = null;
                    if (!string.IsNullOrWhiteSpace(tandiaProduct.Brand))
                    {
                        supplier = await GetOrCreateSupplierAsync(tandiaProduct.Brand);
                    }
                    
                    // Create new product
                    var newProduct = new Product
                    {
                        Code = tandiaProduct.Code,
                        Name = tandiaProduct.Name,
                        Description = tandiaProduct.Description,
                        PurchasePrice = tandiaProduct.CostPrice,
                        SalePrice = tandiaProduct.SalePrice,
                        Stock = tandiaProduct.Stock,
                        MinimumStock = tandiaProduct.MinStock,
                        Unit = tandiaProduct.Unit,
                        CategoryId = category.Id,
                        SupplierId = supplier?.Id,
                        Active = tandiaProduct.Status.ToUpper() == "ACTIVO",
                        ImportBatchId = importBatch.Id
                    };
                    
                    await _productRepository.AddAsync(newProduct);
                    
                    result.SuccessCount++;
                    importBatch.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.ErrorCount++;
                    importBatch.ErrorCount++;
                    result.Errors.Add($"Row {result.SuccessCount + result.ErrorCount}: {ex.Message}");
                }
            }
            
            // Actualizar ImportBatch con resultados finales
            importBatch.Errors = result.Errors.Any() ? System.Text.Json.JsonSerializer.Serialize(result.Errors) : null;
            importBatch.Warnings = result.Warnings.Any() ? System.Text.Json.JsonSerializer.Serialize(result.Warnings) : null;
            await _importBatchRepository.UpdateAsync(importBatch);
            
            // Agregar código de batch al resultado
            result.Warnings.Insert(0, $"Código de lote: {batchCode}");
        }
        catch (Exception ex)
        {
            result.Errors.Add($"File processing error: {ex.Message}");
        }
        
        result.ProcessingTime = DateTime.Now - startTime;
        return result;
    }

    public async Task<BulkUploadResultDto> ImportSalesFromExcelAsync(Stream excelStream, string fileName, string storeCode)
    {
        var startTime = DateTime.Now;
        var result = new BulkUploadResultDto();
        
        try
        {
            // Verificar que existe la sucursal
            var store = await _storeRepository.FirstOrDefaultAsync(s => s.Code == storeCode);
            if (store == null)
            {
                result.Errors.Add($"Sucursal con código '{storeCode}' no encontrada");
                result.ErrorCount++;
                return result;
            }

            // Crear ImportBatch con código único
            var batchCode = await GenerateUniqueBatchCodeAsync("SALES");
            var importBatch = new ImportBatch
            {
                BatchCode = batchCode,
                BatchType = "SALES",
                FileName = fileName,
                StoreCode = storeCode,
                ImportDate = DateTime.UtcNow,
                ImportedBy = "SYSTEM",
                TotalRecords = 0,
                SuccessCount = 0,
                SkippedCount = 0,
                ErrorCount = 0
            };
            
            // Guardar batch para obtener ID
            await _importBatchRepository.AddAsync(importBatch);
            
            var tandiaSales = await ValidateSalesExcelAsync(excelStream);
            result.TotalRecords = tandiaSales.Count;
            
            // Actualizar total records
            importBatch.TotalRecords = tandiaSales.Count;
            
            // Group sales by document number
            var salesGroups = tandiaSales.GroupBy(s => s.DocumentNumber).ToList();
            
            foreach (var salesGroup in salesGroups)
            {
                try
                {
                    var firstSale = salesGroup.First();
                    
                    // Check if sale already exists
                    var existingSale = await _saleRepository.FirstOrDefaultAsync(s => s.SaleNumber == firstSale.DocumentNumber);
                    
                    if (existingSale != null)
                    {
                        result.Warnings.Add($"Sale {firstSale.DocumentNumber} already exists, skipping...");
                        result.SkippedCount++;
                        importBatch.SkippedCount++;
                        continue;
                    }
                    
                    // Get or create customer
                    var customer = await GetOrCreateCustomerAsync(firstSale.CustomerName, firstSale.CustomerDocument);
                    
                    // Create sale
                    var sale = new Sale
                    {
                        SaleNumber = firstSale.DocumentNumber,
                        SaleDate = DateTime.SpecifyKind(firstSale.Date, DateTimeKind.Utc),
                        CustomerId = customer?.Id,
                        StoreId = store.Id, // Asignar la sucursal
                        SubTotal = 0,
                        Taxes = 0,
                        Total = 0,
                        ImportedAt = DateTime.UtcNow, // Marcar como importado
                        ImportSource = batchCode, // Usar código de batch como fuente
                        ImportBatchId = importBatch.Id, // Asociar al batch
                        Details = new List<SaleDetail>()
                    };
                    
                    decimal saleSubTotal = 0;
                    decimal saleTaxes = 0;
                    var movementsToCreate = new List<InventoryMovement>();
                    
                    // First pass: Create sale details and calculate totals
                    foreach (var saleDetail in salesGroup)
                    {
                        // Find product
                        var product = await _productRepository.FirstOrDefaultAsync(p => p.Code == saleDetail.ProductCode);
                        
                        if (product == null)
                        {
                            result.Warnings.Add($"Product {saleDetail.ProductCode} not found for sale {saleDetail.DocumentNumber}");
                            continue;
                        }
                        
                        // Create sale detail
                        var detail = new SaleDetail
                        {
                            ProductId = product.Id,
                            Quantity = saleDetail.Quantity,
                            UnitPrice = saleDetail.SalePrice,
                            Subtotal = saleDetail.Total
                        };
                        
                        sale.Details.Add(detail);
                        saleSubTotal += detail.Subtotal;
                        saleTaxes += saleDetail.Tax;
                    }
                    
                    // Set sale totals and save the sale first to get the ID
                    sale.SubTotal = saleSubTotal;
                    sale.Taxes = saleTaxes;
                    sale.Total = saleSubTotal + saleTaxes;
                    
                    await _saleRepository.AddAsync(sale);
                    
                    // Second pass: Update stock and create movements with correct SaleId
                    foreach (var saleDetail in salesGroup)
                    {
                        // Find product again
                        var product = await _productRepository.FirstOrDefaultAsync(p => p.Code == saleDetail.ProductCode);
                        
                        if (product == null) continue; // Already warned in first pass
                        
                        // Use the store selected by the user (not from Excel warehouse column)
                        // Get or create product stock for this store
                        var productStock = await GetOrCreateProductStockAsync(product.Id, store.Id);
                        
                        // Update product stock and register movement
                        var previousStock = productStock.CurrentStock;
                        productStock.CurrentStock -= saleDetail.Quantity;
                        await _productStockRepository.UpdateAsync(productStock);
                        
                        // Also update legacy Product.Stock for backward compatibility
                        product.Stock -= saleDetail.Quantity;
                        await _productRepository.UpdateAsync(product);
                        
                        // Register inventory movement with correct SaleId
                        var movement = new InventoryMovement
                        {
                            Date = DateTime.SpecifyKind(firstSale.Date, DateTimeKind.Utc),
                            Type = MovementType.Sale,
                            Quantity = -saleDetail.Quantity, // Negative for outbound
                            Reason = $"Sale imported from Tandia - {firstSale.DocumentNumber}",
                            PreviousStock = previousStock,
                            NewStock = productStock.CurrentStock,
                            DocumentNumber = firstSale.DocumentNumber,
                            UserName = "Tandia_Import",
                            Source = "Tandia_Import",
                            UnitCost = saleDetail.SalePrice,
                            TotalCost = saleDetail.Total,
                            ProductId = product.Id,
                            StoreId = store.Id,
                            ProductStockId = productStock.Id,
                            SaleId = sale.Id // Now we have the correct Sale ID!
                        };
                        
                        await _inventoryMovementRepository.AddAsync(movement);
                    }
                    result.SuccessCount++;
                    importBatch.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.ErrorCount++;
                    importBatch.ErrorCount++;
                    result.Errors.Add($"Sale {salesGroup.Key}: {ex.Message}");
                }
            }
            
            // Actualizar ImportBatch con resultados finales
            importBatch.Errors = result.Errors.Any() ? System.Text.Json.JsonSerializer.Serialize(result.Errors) : null;
            importBatch.Warnings = result.Warnings.Any() ? System.Text.Json.JsonSerializer.Serialize(result.Warnings) : null;
            await _importBatchRepository.UpdateAsync(importBatch);
            
            // Agregar código de batch al resultado
            result.Warnings.Insert(0, $"Código de lote: {batchCode}");
        }
        catch (Exception ex)
        {
            result.Errors.Add($"File processing error: {ex.Message}");
        }
        
        result.ProcessingTime = DateTime.Now - startTime;
        return result;
    }

    public async Task<TandiaUploadSummaryDto> ImportFullDatasetAsync(Stream productsStream, Stream salesStream)
    {
        var summary = new TandiaUploadSummaryDto
        {
            UploadDate = DateTime.Now,
            UploadedBy = "System" // This should come from authentication context
        };
        
        // Import products first
        summary.ProductsResult = await ImportProductsFromExcelAsync(productsStream, "products.xlsx");
        
        // Then import sales (using default store for legacy compatibility)
        var allStores = await _storeRepository.GetAllAsync();
        var defaultStore = allStores.FirstOrDefault();
        if (defaultStore == null)
        {
            throw new InvalidOperationException("No hay almacenes configurados en el sistema");
        }
        summary.SalesResult = await ImportSalesFromExcelAsync(salesStream, "sales.xlsx", defaultStore.Code);
        
        return summary;
    }
    
    // Helper methods
    private string GetCellValue(IXLWorksheet worksheet, int row, int col)
    {
        var cell = worksheet.Cell(row, col);
        return cell.Value.ToString().Trim() ?? string.Empty;
    }
    
    private decimal GetDecimalValue(IXLWorksheet worksheet, int row, int col)
    {
        var cell = worksheet.Cell(row, col);
        if (cell.IsEmpty()) return 0;
        
        if (decimal.TryParse(cell.Value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
            return result;
            
        return 0;
    }
    
    private int GetIntValue(IXLWorksheet worksheet, int row, int col)
    {
        var cell = worksheet.Cell(row, col);
        if (cell.IsEmpty()) return 0;
        
        if (int.TryParse(cell.Value.ToString(), out int result))
            return result;
            
        return 0;
    }
    
    private DateTime GetDateValue(IXLWorksheet worksheet, int row, int col)
    {
        var cell = worksheet.Cell(row, col);
        if (cell.IsEmpty()) return DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        
        // ClosedXML can handle dates directly
        if (cell.DataType == XLDataType.DateTime)
            return DateTime.SpecifyKind(cell.GetDateTime(), DateTimeKind.Utc);
            
        if (DateTime.TryParse(cell.Value.ToString(), out DateTime result))
            return DateTime.SpecifyKind(result, DateTimeKind.Utc);
            
        return DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
    }
    
    private TimeSpan GetTimeValue(IXLWorksheet worksheet, int row, int col)
    {
        var cell = worksheet.Cell(row, col);
        if (cell.IsEmpty()) return TimeSpan.Zero;
        
        // Try to get as TimeSpan first
        if (cell.DataType == XLDataType.TimeSpan)
            return cell.GetTimeSpan();
            
        if (TimeSpan.TryParse(cell.Value.ToString(), out TimeSpan result))
            return result;
            
        return TimeSpan.Zero;
    }
    
    private async Task<Category> GetOrCreateCategoryAsync(string categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
            categoryName = "Uncategorized";
            
        var category = await _categoryRepository.FirstOrDefaultAsync(c => c.Name == categoryName);
        
        if (category == null)
        {
            category = new Category
            {
                Name = categoryName,
                Description = $"Category imported from Tandia: {categoryName}",
                Active = true
            };
            
            await _categoryRepository.AddAsync(category);
        }
        
        return category;
    }
    
    private async Task<Supplier> GetOrCreateSupplierAsync(string supplierName)
    {
        if (string.IsNullOrWhiteSpace(supplierName))
            return null!;
            
        var supplier = await _supplierRepository.FirstOrDefaultAsync(s => s.Name == supplierName);
        
        if (supplier == null)
        {
            supplier = new Supplier
            {
                Name = supplierName,
                Active = true
            };
            
            await _supplierRepository.AddAsync(supplier);
        }
        
        return supplier;
    }
    
    private async Task<Customer?> GetOrCreateCustomerAsync(string customerName, string customerDoc)
    {
        if (string.IsNullOrWhiteSpace(customerName) || customerName == "Generic Customer" || customerName == "Cliente Genérico")
            return null;
            
        var customer = await _customerRepository.FirstOrDefaultAsync(c => c.Document == customerDoc);
        
        if (customer == null)
        {
            customer = new Customer
            {
                Name = customerName,
                Document = customerDoc,
                Active = true
            };
            
            await _customerRepository.AddAsync(customer);
        }
        
        return customer;
    }
    
    private async Task<Store> GetOrCreateStoreAsync(string storeName)
    {
        if (string.IsNullOrWhiteSpace(storeName))
            storeName = "Main Warehouse";
            
        var store = await _storeRepository.GetByNameAsync(storeName);
        
        if (store == null)
        {
            var code = storeName.Length > 4 ? storeName.Substring(0, 4).ToUpper() : storeName.ToUpper();
            
            store = new Store
            {
                Code = code,
                Name = storeName,
                Description = $"Store imported from Tandia: {storeName}",
                Active = true
            };
            
            await _storeRepository.AddAsync(store);
        }
        
        return store;
    }
    
    private async Task<ProductStock> GetOrCreateProductStockAsync(int productId, int storeId)
    {
        var productStock = await _productStockRepository.GetByProductAndStoreAsync(productId, storeId);
        
        if (productStock == null)
        {
            productStock = new ProductStock
            {
                ProductId = productId,
                StoreId = storeId,
                CurrentStock = 0,
                MinimumStock = 0,
                MaximumStock = 0,
                AverageCost = 0
            };
            
            await _productStockRepository.AddAsync(productStock);
        }
        
        return productStock;
    }
    
    public async Task<ClearDataResultDto> ClearAllProductsAsync()
    {
        var result = new ClearDataResultDto();
        
        // Eliminar movimientos de inventario relacionados con productos
        var inventoryMovements = await _inventoryMovementRepository.GetAllAsync();
        foreach (var movement in inventoryMovements)
        {
            await _inventoryMovementRepository.DeleteAsync(movement.Id);
            result.DeletedInventoryMovements++;
        }
        
        // Eliminar stocks de productos
        var productStocks = await _productStockRepository.GetAllAsync();
        foreach (var stock in productStocks)
        {
            await _productStockRepository.DeleteAsync(stock.Id);
            result.DeletedProductStocks++;
        }
        
        // Eliminar productos
        var products = await _productRepository.GetAllAsync();
        foreach (var product in products)
        {
            await _productRepository.DeleteAsync(product.Id);
            result.DeletedProducts++;
        }
        
        // Eliminar categorías
        var categories = await _categoryRepository.GetAllAsync();
        foreach (var category in categories)
        {
            await _categoryRepository.DeleteAsync(category.Id);
            result.DeletedCategories++;
        }
        
        result.Message = "Todos los productos y datos relacionados han sido eliminados exitosamente";
        return result;
    }
    
    public async Task<ClearDataResultDto> ClearAllSalesAsync()
    {
        var result = new ClearDataResultDto();
        
        // Eliminar movimientos de inventario relacionados con ventas
        var allMovements = await _inventoryMovementRepository.GetAllAsync();
        var salesMovements = allMovements.Where(m => 
            m.Type == Core.Entities.MovementType.Sale || 
            m.Type == Core.Entities.MovementType.TandiaImport_Sale).ToList();
        foreach (var movement in salesMovements)
        {
            await _inventoryMovementRepository.DeleteAsync(movement.Id);
            result.DeletedInventoryMovements++;
        }
        
        // Eliminar todas las ventas (esto eliminará automáticamente los detalles por cascada)
        var sales = await _saleRepository.GetAllAsync();
        foreach (var sale in sales)
        {
            // Contar detalles antes de eliminar
            var saleDetails = await _saleRepository.GetSaleDetailsAsync(sale.Id);
            result.DeletedSaleDetails += saleDetails?.Details?.Count ?? 0;
            
            await _saleRepository.DeleteAsync(sale.Id);
            result.DeletedSales++;
        }
        
        result.Message = "Todas las ventas y datos relacionados han sido eliminados exitosamente";
        return result;
    }

    public async Task<int> DeleteProductsByBatchIdAsync(int batchId)
    {
        int deletedCount = 0;
        
        // Obtener todos los productos que pertenecen a este batch
        var allProducts = await _productRepository.GetAllAsync();
        var batchProducts = allProducts.Where(p => p.ImportBatchId == batchId).ToList();
        
        foreach (var product in batchProducts)
        {
            // Eliminar movimientos de inventario relacionados con este producto
            var allMovements = await _inventoryMovementRepository.GetAllAsync();
            var productMovements = allMovements.Where(m => m.ProductId == product.Id).ToList();
            foreach (var movement in productMovements)
            {
                await _inventoryMovementRepository.DeleteAsync(movement.Id);
            }
            
            // Eliminar stocks del producto
            var allStocks = await _productStockRepository.GetAllAsync();
            var productStocks = allStocks.Where(s => s.ProductId == product.Id).ToList();
            foreach (var stock in productStocks)
            {
                await _productStockRepository.DeleteAsync(stock.Id);
            }
            
            // Eliminar el producto
            await _productRepository.DeleteAsync(product.Id);
            deletedCount++;
        }
        
        return deletedCount;
    }

    public async Task<int> DeleteSalesByBatchIdAsync(int batchId)
    {
        var deletedCount = 0;
        
        // Obtener todas las ventas de este batch con sus details incluidos
        var allSales = await _saleRepository.GetAllAsync();
        var batchSales = allSales.Where(s => s.ImportBatchId == batchId).ToList();
        
        // Cargar details para cada venta (si no están cargados)
        foreach (var sale in batchSales)
        {
            var saleWithDetails = await _saleRepository.GetSaleDetailsAsync(sale.Id);
            if (saleWithDetails != null)
            {
                sale.Details = saleWithDetails.Details;
            }
        }
        
        foreach (var sale in batchSales)
        {
            // 1. Revertir movimientos de inventario (devolver stock)
            var allMovements = await _inventoryMovementRepository.GetAllAsync();
            var saleMovements = allMovements.Where(m => 
                m.SaleId == sale.Id && 
                (m.Type == MovementType.Sale || m.Type == MovementType.TandiaImport_Sale)
            ).ToList();
            
            foreach (var movement in saleMovements)
            {
                // Devolver el stock al ProductStock correspondiente
                var productStock = await _productStockRepository.GetByProductAndStoreAsync(movement.ProductId, movement.StoreId);
                if (productStock != null)
                {
                    // Revertir la salida de stock (sumar la cantidad que se había restado)
                    productStock.CurrentStock += movement.Quantity;
                    await _productStockRepository.UpdateAsync(productStock);
                }
                
                // Eliminar el movimiento
                await _inventoryMovementRepository.DeleteAsync(movement.Id);
            }
            
            // 2. Los SaleDetails se eliminarán automáticamente por CASCADE DELETE
            // al eliminar la Sale (configurado en EF Core)
            
            // 3. Eliminar la Sale
            await _saleRepository.DeleteAsync(sale.Id);
            deletedCount++;
        }
        
        return deletedCount;
    }
}