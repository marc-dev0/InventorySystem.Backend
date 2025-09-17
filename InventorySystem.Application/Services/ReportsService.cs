using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using InventorySystem.Application.DTOs.Reports;
using InventorySystem.Application.Helpers;
using InventorySystem.Application.Interfaces;
using InventorySystem.Core.Interfaces;
using StorePerformanceDto = InventorySystem.Application.Interfaces.StorePerformanceDto;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using ClosedXML.Excel;

namespace InventorySystem.Application.Services
{
    public class ReportsService : IReportsService
    {
        private readonly IProductStockRepository _productStockRepository;
        private readonly ISaleRepository _saleRepository;
        private readonly IConfigurationService _configurationService;

        public ReportsService(
            IProductStockRepository productStockRepository,
            ISaleRepository saleRepository,
            IConfigurationService configurationService)
        {
            _productStockRepository = productStockRepository;
            _saleRepository = saleRepository;
            _configurationService = configurationService;
        }

        public async Task<StockCriticalReportDto> GetStockCriticalReportAsync(ReportFiltersDto filters)
        {
            var globalMinimumStock = await _configurationService.GetGlobalMinimumStockAsync();

            // Get all product stocks with product and store information
            var allStocks = await _productStockRepository.GetAllAsync();

            // Apply filters
            var filteredStocks = allStocks.Where(ps => ps.Product.Active && !ps.IsDeleted);

            if (!string.IsNullOrEmpty(filters.StoreCode))
                filteredStocks = filteredStocks.Where(ps => ps.Store.Code == filters.StoreCode);

            if (filters.CategoryId.HasValue)
                filteredStocks = filteredStocks.Where(ps => ps.Product.CategoryId == filters.CategoryId);

            if (filters.BrandId.HasValue)
                filteredStocks = filteredStocks.Where(ps => ps.Product.BrandId == filters.BrandId);

            var inventoryItems = filteredStocks.Select(ps => new InventoryReportDto
            {
                ProductId = ps.ProductId,
                ProductCode = ps.Product.Code,
                ProductName = ps.Product.Name,
                CategoryName = ps.Product.Category?.Name ?? "",
                BrandName = ps.Product.Brand?.Name ?? "",
                StoreCode = ps.Store.Code,
                StoreName = ps.Store.Name,
                CurrentStock = ps.CurrentStock,
                MinimumStock = ps.Product.MinimumStock > 0 ? ps.Product.MinimumStock : globalMinimumStock,
                PurchasePrice = ps.Product.PurchasePrice,
                SalePrice = ps.Product.SalePrice,
                TotalValue = ps.CurrentStock * ps.Product.PurchasePrice,
                IsLowStock = ps.CurrentStock <= (ps.Product.MinimumStock > 0 ? ps.Product.MinimumStock : globalMinimumStock),
                IsOutOfStock = ps.CurrentStock <= 0
            }).ToList();

            var lowStockItems = inventoryItems.Where(i => i.IsLowStock && !i.IsOutOfStock).ToList();
            var outOfStockItems = inventoryItems.Where(i => i.IsOutOfStock).ToList();

            return new StockCriticalReportDto
            {
                LowStockItems = lowStockItems,
                OutOfStockItems = outOfStockItems,
                TotalLowStockValue = lowStockItems.Sum(i => i.TotalValue),
                TotalLowStockItems = lowStockItems.Count,
                TotalOutOfStockItems = outOfStockItems.Count
            };
        }

        public async Task<List<InventoryValuationReportDto>> GetInventoryValuationReportAsync(ReportFiltersDto filters)
        {
            // Get all product stocks with product and store information
            var allStocks = await _productStockRepository.GetAllAsync();

            // Apply filters
            var filteredStocks = allStocks.Where(ps => ps.Product.Active && !ps.IsDeleted);

            if (!string.IsNullOrEmpty(filters.StoreCode))
                filteredStocks = filteredStocks.Where(ps => ps.Store.Code == filters.StoreCode);

            var valuation = filteredStocks
                .GroupBy(ps => new {
                    CategoryName = ps.Product.Category?.Name ?? "Sin Categoría",
                    BrandName = ps.Product.Brand?.Name ?? "Sin Marca",
                    StoreName = ps.Store.Name
                })
                .Select(g => new InventoryValuationReportDto
                {
                    CategoryName = g.Key.CategoryName,
                    BrandName = g.Key.BrandName,
                    StoreName = g.Key.StoreName,
                    TotalUnits = g.Sum(ps => ps.CurrentStock),
                    TotalPurchaseValue = g.Sum(ps => ps.CurrentStock * ps.Product.PurchasePrice),
                    TotalSaleValue = g.Sum(ps => ps.CurrentStock * ps.Product.SalePrice),
                    ProductCount = g.Count()
                })
                .ToList();

            valuation.ForEach(v => v.PotentialProfit = v.TotalSaleValue - v.TotalPurchaseValue);

            return valuation;
        }

        public async Task<SalesPeriodReportDto> GetSalesPeriodReportAsync(ReportFiltersDto filters)
        {
            var startDate = filters.StartDate ?? DateTime.Now.AddDays(-30);
            var endDate = filters.EndDate ?? DateTime.Now;

            // Get sales within the date range
            var allSales = await _saleRepository.GetAllAsync();

            var filteredSales = allSales.Where(s =>
                s.SaleDate.Date >= startDate.Date && s.SaleDate.Date <= endDate.Date);

            if (!string.IsNullOrEmpty(filters.StoreCode))
                filteredSales = filteredSales.Where(s => s.Store.Code == filters.StoreCode);

            var salesList = filteredSales.ToList();

            // Calculate totals
            var totalSales = salesList.Sum(s => s.Total);
            var totalTransactions = salesList.Count;
            var averageTicket = totalTransactions > 0 ? totalSales / totalTransactions : 0;

            // Calculate daily sales
            var dailySales = salesList
                .GroupBy(s => s.SaleDate.Date)
                .Select(g => new DailySalesDto
                {
                    Date = g.Key,
                    TotalSales = g.Sum(s => s.Total),
                    TotalProfit = g.Sum(s => s.Total * 0.2m), // Assuming 20% profit margin
                    TransactionCount = g.Count()
                })
                .OrderBy(d => d.Date)
                .ToList();

            return new SalesPeriodReportDto
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalSales = totalSales,
                TotalCost = totalSales * 0.8m, // Assuming 80% cost
                TotalProfit = totalSales * 0.2m, // Assuming 20% profit
                AverageTicket = averageTicket,
                TotalTransactions = totalTransactions,
                TotalItems = (int)salesList.SelectMany(s => s.Details).Sum(sd => sd.Quantity),
                DailySales = dailySales,
                TopProducts = new List<TopProductDto>(), // TODO: Implement if needed
                SalesByCategory = new List<CategorySalesDto>() // TODO: Implement if needed
            };
        }

        // Placeholder implementations for other methods
        public Task<List<ProductMovementReportDto>> GetProductMovementReportAsync(ReportFiltersDto filters)
        {
            // TODO: Implement based on InventoryMovement entity
            return Task.FromResult(new List<ProductMovementReportDto>());
        }

        public async Task<List<InventoryReportDto>> GetProductsWithoutMovementReportAsync(ReportFiltersDto filters)
        {
            var daysThreshold = filters.DaysThreshold ?? 30; // Default to 30 days
            var thresholdDate = DateTime.Now.AddDays(-daysThreshold);

            var allSales = await _saleRepository.GetAllAsync();
            var allStocks = await _productStockRepository.GetAllAsync();

            // Get products that had sales within the threshold period
            var productsWithRecentSales = allSales
                .Where(s => s.SaleDate >= thresholdDate)
                .SelectMany(s => s.Details)
                .Select(d => d.ProductId)
                .Distinct()
                .ToHashSet();

            // Filter stocks to find products without recent movement
            var filteredStocks = allStocks.Where(ps => ps.Product.Active && !ps.IsDeleted);

            if (!string.IsNullOrEmpty(filters.StoreCode))
                filteredStocks = filteredStocks.Where(ps => ps.Store.Code == filters.StoreCode);

            var productsWithoutMovement = filteredStocks
                .Where(ps => !productsWithRecentSales.Contains(ps.ProductId))
                .Select(ps => new InventoryReportDto
                {
                    ProductId = ps.ProductId,
                    ProductCode = ps.Product.Code,
                    ProductName = ps.Product.Name,
                    CategoryName = ps.Product.Category?.Name ?? "",
                    BrandName = ps.Product.Brand?.Name ?? "",
                    StoreCode = ps.Store.Code,
                    StoreName = ps.Store.Name,
                    CurrentStock = ps.CurrentStock,
                    MinimumStock = ps.Product.MinimumStock,
                    PurchasePrice = ps.Product.PurchasePrice,
                    SalePrice = ps.Product.SalePrice,
                    TotalValue = ps.CurrentStock * ps.Product.PurchasePrice,
                    IsLowStock = false,
                    IsOutOfStock = ps.CurrentStock <= 0,
                    LastMovementDate = DateTime.Now.AddDays(-daysThreshold), // Placeholder
                    DaysWithoutMovement = daysThreshold
                })
                .OrderByDescending(p => p.TotalValue)
                .ToList();

            return productsWithoutMovement;
        }

        public async Task<List<TopProductDto>> GetTopProductsReportAsync(ReportFiltersDto filters, int topCount = 10)
        {
            var startDate = filters.StartDate ?? DateTime.Now.AddDays(-30);
            var endDate = filters.EndDate ?? DateTime.Now;

            var allSales = await _saleRepository.GetAllAsync();

            var filteredSales = allSales.Where(s =>
                s.SaleDate.Date >= startDate.Date && s.SaleDate.Date <= endDate.Date);

            if (!string.IsNullOrEmpty(filters.StoreCode))
                filteredSales = filteredSales.Where(s => s.Store.Code == filters.StoreCode);

            var topProducts = filteredSales
                .SelectMany(s => s.Details)
                .GroupBy(d => new { d.Product.Code, d.Product.Name })
                .Select(g => new TopProductDto
                {
                    ProductCode = g.Key.Code,
                    ProductName = g.Key.Name,
                    QuantitySold = g.Sum(d => d.Quantity),
                    TotalRevenue = g.Sum(d => d.Quantity * d.UnitPrice),
                    TotalProfit = g.Sum(d => d.Quantity * (d.UnitPrice - d.Product.PurchasePrice)),
                    TransactionCount = g.Select(d => d.SaleId).Distinct().Count()
                })
                .OrderByDescending(p => p.TotalRevenue)
                .Take(topCount)
                .ToList();

            return topProducts;
        }

        public async Task<List<CategorySalesDto>> GetSalesByCategoryReportAsync(ReportFiltersDto filters)
        {
            var startDate = filters.StartDate ?? DateTime.Now.AddDays(-30);
            var endDate = filters.EndDate ?? DateTime.Now;

            var allSales = await _saleRepository.GetAllAsync();

            var filteredSales = allSales.Where(s =>
                s.SaleDate.Date >= startDate.Date && s.SaleDate.Date <= endDate.Date);

            if (!string.IsNullOrEmpty(filters.StoreCode))
                filteredSales = filteredSales.Where(s => s.Store.Code == filters.StoreCode);

            var categorySales = filteredSales
                .SelectMany(s => s.Details)
                .GroupBy(d => d.Product.Category?.Name ?? "Sin Categoría")
                .Select(g => new CategorySalesDto
                {
                    CategoryName = g.Key,
                    TotalSales = g.Sum(d => d.Quantity * d.UnitPrice),
                    TotalProfit = g.Sum(d => d.Quantity * (d.UnitPrice - d.Product.PurchasePrice)),
                    QuantitySold = g.Sum(d => d.Quantity),
                    ProductCount = g.Select(d => d.Product.Id).Distinct().Count()
                })
                .OrderByDescending(c => c.TotalSales)
                .ToList();

            return categorySales;
        }

        public async Task<List<CustomerAnalysisDto>> GetCustomerAnalysisReportAsync(ReportFiltersDto filters)
        {
            var startDate = filters.StartDate ?? DateTime.Now.AddDays(-365); // Last year by default
            var endDate = filters.EndDate ?? DateTime.Now;

            var allSales = await _saleRepository.GetAllAsync();

            var filteredSales = allSales.Where(s =>
                s.SaleDate >= startDate && s.SaleDate <= endDate && s.Customer != null);

            if (!string.IsNullOrEmpty(filters.StoreCode))
                filteredSales = filteredSales.Where(s => s.Store.Code == filters.StoreCode);

            var customerAnalysis = filteredSales
                .GroupBy(s => new { s.Customer.Id, s.Customer.Name, s.Customer.Email })
                .Select(g => new CustomerAnalysisDto
                {
                    CustomerId = g.Key.Id,
                    CustomerName = g.Key.Name,
                    Email = g.Key.Email ?? "",
                    TotalPurchases = g.Sum(s => s.Total),
                    TransactionCount = g.Count(),
                    AverageTicket = g.Average(s => s.Total),
                    FirstPurchase = g.Min(s => s.SaleDate),
                    LastPurchase = g.Max(s => s.SaleDate),
                    DaysSinceLastPurchase = (int)(DateTime.Now - g.Max(s => s.SaleDate)).TotalDays
                })
                .OrderByDescending(c => c.TotalPurchases)
                .ToList();

            return customerAnalysis;
        }

        public Task<List<SalesReportDto>> GetEmployeeSalesReportAsync(ReportFiltersDto filters)
        {
            // TODO: Implement
            return Task.FromResult(new List<SalesReportDto>());
        }

        public async Task<List<StorePerformanceDto>> GetStorePerformanceReportAsync(ReportFiltersDto filters)
        {
            var startDate = filters.StartDate ?? DateTime.Now.AddDays(-30);
            var endDate = filters.EndDate ?? DateTime.Now;

            var allSales = await _saleRepository.GetAllAsync();
            var allStocks = await _productStockRepository.GetAllAsync();

            var filteredSales = allSales.Where(s =>
                s.SaleDate.Date >= startDate.Date && s.SaleDate.Date <= endDate.Date);

            var storePerformance = filteredSales
                .GroupBy(s => new { s.Store.Code, s.Store.Name })
                .Select(g => new StorePerformanceDto
                {
                    StoreCode = g.Key.Code,
                    StoreName = g.Key.Name,
                    TotalSales = g.Sum(s => s.Total),
                    TotalCost = g.Sum(s => s.Total * 0.8m), // Assuming 80% cost
                    TotalProfit = g.Sum(s => s.Total * 0.2m), // Assuming 20% profit
                    TransactionCount = g.Count(),
                    AverageTicket = g.Average(s => s.Total),
                    ProductCount = allStocks.Where(ps => ps.Store.Code == g.Key.Code).Count(),
                    InventoryValue = allStocks.Where(ps => ps.Store.Code == g.Key.Code)
                                              .Sum(ps => ps.CurrentStock * ps.Product.PurchasePrice),
                    EmployeeCount = 1, // TODO: Implement when Employee system is available
                    SalesPerEmployee = g.Sum(s => s.Total) / 1 // TODO: Calculate properly
                })
                .ToList();

            // Calculate profit margin
            foreach (var store in storePerformance)
            {
                store.ProfitMargin = store.TotalSales > 0 ? (store.TotalProfit / store.TotalSales) * 100 : 0;
            }

            return storePerformance.OrderByDescending(s => s.TotalSales).ToList();
        }

        public Task<List<InventoryReportDto>> GetStoreStockReportAsync(ReportFiltersDto filters)
        {
            // TODO: Implement
            return Task.FromResult(new List<InventoryReportDto>());
        }

        public async Task<byte[]> ExportReportAsync(string reportType, ReportFiltersDto filters, ReportExportOptionsDto exportOptions)
        {
            var format = exportOptions.Format.ToLower();

            switch (format)
            {
                case "csv":
                    return await GenerateCsvReportAsync(reportType, filters);
                case "excel":
                case "xlsx":
                case "xls":
                    return await GenerateExcelReportAsync(reportType, filters);
                case "pdf":
                    return await GeneratePdfReportAsync(reportType, filters);
                default:
                    return await GenerateCsvReportAsync(reportType, filters);
            }
        }

        private async Task<string> GenerateReportContentAsync(string reportType, ReportFiltersDto filters)
        {
            switch (reportType.ToLower())
            {
                case "stock-critical":
                    return await GenerateStockCriticalContent(filters);
                case "inventory-valuation":
                    return await GenerateInventoryValuationContent(filters);
                case "sales-period":
                    return await GenerateSalesPeriodContent(filters);
                default:
                    return "<p>Tipo de reporte no implementado.</p>";
            }
        }

        private async Task<string> GenerateStockCriticalContent(ReportFiltersDto filters)
        {
            var report = await GetStockCriticalReportAsync(filters);

            var html = $@"
    <div class='summary'>
        <h3>Resumen de Stock Crítico</h3>
        <p><strong>Productos con stock bajo:</strong> {report.TotalLowStockItems}</p>
        <p><strong>Productos sin stock:</strong> {report.TotalOutOfStockItems}</p>
        <p><strong>Valor en riesgo:</strong> {CurrencyHelper.FormatCurrency(report.TotalLowStockValue)}</p>
    </div>

    <h3>Productos con Stock Bajo</h3>
    <table>
        <thead>
            <tr>
                <th>Código</th>
                <th>Producto</th>
                <th>Stock Actual</th>
                <th>Stock Mínimo</th>
                <th>Tienda</th>
            </tr>
        </thead>
        <tbody>";

            foreach (var item in report.LowStockItems)
            {
                html += $@"
            <tr>
                <td>{item.ProductCode}</td>
                <td>{item.ProductName}</td>
                <td>{item.CurrentStock}</td>
                <td>{item.MinimumStock}</td>
                <td>{item.StoreName}</td>
            </tr>";
            }

            html += "</tbody></table>";
            return html;
        }

        private async Task<string> GenerateInventoryValuationContent(ReportFiltersDto filters)
        {
            var report = await GetInventoryValuationReportAsync(filters);

            var totalValue = report.Sum(r => r.TotalPurchaseValue);
            var totalProfit = report.Sum(r => r.PotentialProfit);

            var html = $@"
    <div class='summary'>
        <h3>Valorización de Inventario</h3>
        <p><strong>Valor total inventario:</strong> {CurrencyHelper.FormatCurrency(totalValue)}</p>
        <p><strong>Ganancia potencial:</strong> {CurrencyHelper.FormatCurrency(totalProfit)}</p>
        <p><strong>Categorías analizadas:</strong> {report.Count}</p>
    </div>

    <h3>Valorización por Categoría</h3>
    <table>
        <thead>
            <tr>
                <th>Categoría</th>
                <th>Valor Compra</th>
                <th>Ganancia Potencial</th>
                <th>Productos</th>
            </tr>
        </thead>
        <tbody>";

            foreach (var item in report)
            {
                html += $@"
            <tr>
                <td>{item.CategoryName}</td>
                <td>{CurrencyHelper.FormatCurrency(item.TotalPurchaseValue)}</td>
                <td>{CurrencyHelper.FormatCurrency(item.PotentialProfit)}</td>
                <td>{item.ProductCount}</td>
            </tr>";
            }

            html += "</tbody></table>";
            return html;
        }

        private async Task<string> GenerateSalesPeriodContent(ReportFiltersDto filters)
        {
            var report = await GetSalesPeriodReportAsync(filters);

            var html = $@"
    <div class='summary'>
        <h3>Reporte de Ventas por Período</h3>
        <p><strong>Ventas totales:</strong> {CurrencyHelper.FormatCurrency(report.TotalSales)}</p>
        <p><strong>Total transacciones:</strong> {report.TotalTransactions}</p>
        <p><strong>Ticket promedio:</strong> {CurrencyHelper.FormatCurrency(report.AverageTicket)}</p>
        <p><strong>Ganancia total:</strong> {CurrencyHelper.FormatCurrency(report.TotalProfit)}</p>
        <p><strong>Total items vendidos:</strong> {report.TotalItems}</p>
    </div>

    <h3>Ventas por Día</h3>
    <table>
        <thead>
            <tr>
                <th>Fecha</th>
                <th>Ventas</th>
                <th>Transacciones</th>
                <th>Ganancia</th>
            </tr>
        </thead>
        <tbody>";

            foreach (var day in report.DailySales)
            {
                html += $@"
            <tr>
                <td>{day.Date:dd/MM/yyyy}</td>
                <td>{CurrencyHelper.FormatCurrency(day.TotalSales)}</td>
                <td>{day.TransactionCount}</td>
                <td>{CurrencyHelper.FormatCurrency(day.TotalProfit)}</td>
            </tr>";
            }

            html += "</tbody></table>";
            return html;
        }

        private async Task<byte[]> GenerateCsvReportAsync(string reportType, ReportFiltersDto filters)
        {
            var csvContent = await GenerateCsvContentAsync(reportType, filters);
            return System.Text.Encoding.UTF8.GetBytes(csvContent);
        }

        private async Task<byte[]> GenerateExcelReportAsync(string reportType, ReportFiltersDto filters)
        {
            using var workbook = new XLWorkbook();

            switch (reportType.ToLower())
            {
                case "stock-critical":
                    await AddStockCriticalExcelContent(workbook, filters);
                    break;
                case "inventory-valuation":
                    await AddInventoryValuationExcelContent(workbook, filters);
                    break;
                case "sales-period":
                    await AddSalesPeriodExcelContent(workbook, filters);
                    break;
                case "top-products":
                    await AddTopProductsExcelContent(workbook, filters);
                    break;
                case "sales-by-category":
                    await AddSalesByCategoryExcelContent(workbook, filters);
                    break;
                case "customer-analysis":
                    await AddCustomerAnalysisExcelContent(workbook, filters);
                    break;
                case "store-performance":
                    await AddStorePerformanceExcelContent(workbook, filters);
                    break;
                case "products-without-movement":
                    await AddProductsWithoutMovementExcelContent(workbook, filters);
                    break;
                default:
                    var worksheet = workbook.Worksheets.Add("Reporte");
                    worksheet.Cell(1, 1).Value = "Tipo de reporte no implementado";
                    break;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private async Task<byte[]> GeneratePdfReportAsync(string reportType, ReportFiltersDto filters)
        {
            using var memoryStream = new MemoryStream();

            // Create PDF writer and document
            using var writer = new PdfWriter(memoryStream);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf);

            // Add title
            var title = new Paragraph($"Sistema de Inventario - Reporte {reportType}")
                .SetFontSize(16)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(20);
            document.Add(title);

            // Add metadata
            var metadata = new Paragraph($"Fecha de generación: {DateTime.Now:dd/MM/yyyy HH:mm}\n" +
                                       $"Filtros aplicados:\n" +
                                       $"- Fecha inicio: {filters.StartDate?.ToString("dd/MM/yyyy") ?? "N/A"}\n" +
                                       $"- Fecha fin: {filters.EndDate?.ToString("dd/MM/yyyy") ?? "N/A"}\n" +
                                       $"- Tienda: {filters.StoreCode ?? "Todas"}")
                .SetFontSize(10)
                .SetMarginBottom(20);
            document.Add(metadata);

            // Add report content based on type
            switch (reportType.ToLower())
            {
                case "stock-critical":
                    await AddStockCriticalPdfContent(document, filters);
                    break;
                case "inventory-valuation":
                    await AddInventoryValuationPdfContent(document, filters);
                    break;
                case "sales-period":
                    await AddSalesPeriodPdfContent(document, filters);
                    break;
                case "top-products":
                    await AddTopProductsPdfContent(document, filters);
                    break;
                case "sales-by-category":
                    await AddSalesByCategoryPdfContent(document, filters);
                    break;
                case "customer-analysis":
                    await AddCustomerAnalysisPdfContent(document, filters);
                    break;
                case "store-performance":
                    await AddStorePerformancePdfContent(document, filters);
                    break;
                case "products-without-movement":
                    await AddProductsWithoutMovementPdfContent(document, filters);
                    break;
                default:
                    document.Add(new Paragraph("Tipo de reporte no implementado."));
                    break;
            }

            document.Close();
            return memoryStream.ToArray();
        }

        private async Task<string> GenerateCsvContentAsync(string reportType, ReportFiltersDto filters)
        {
            switch (reportType.ToLower())
            {
                case "stock-critical":
                    return await GenerateStockCriticalCsv(filters);
                case "inventory-valuation":
                    return await GenerateInventoryValuationCsv(filters);
                case "sales-period":
                    return await GenerateSalesPeriodCsv(filters);
                default:
                    return "Código,Producto,Stock Actual,Stock Mínimo,Tienda\n";
            }
        }

        private async Task<string> GenerateStockCriticalCsv(ReportFiltersDto filters)
        {
            var report = await GetStockCriticalReportAsync(filters);

            var csv = "Código,Producto,Categoría,Marca,Tienda,Stock Actual,Stock Mínimo,Estado\n";

            foreach (var item in report.LowStockItems)
            {
                csv += $"\"{item.ProductCode}\",\"{item.ProductName}\",\"{item.CategoryName}\",\"{item.BrandName}\",\"{item.StoreName}\",{item.CurrentStock},{item.MinimumStock},\"Stock Bajo\"\n";
            }

            foreach (var item in report.OutOfStockItems)
            {
                csv += $"\"{item.ProductCode}\",\"{item.ProductName}\",\"{item.CategoryName}\",\"{item.BrandName}\",\"{item.StoreName}\",{item.CurrentStock},{item.MinimumStock},\"Sin Stock\"\n";
            }

            return csv;
        }

        private async Task<string> GenerateInventoryValuationCsv(ReportFiltersDto filters)
        {
            var report = await GetInventoryValuationReportAsync(filters);

            var csv = "Categoría,Marca,Tienda,Total Unidades,Valor Compra,Valor Venta,Ganancia Potencial,Cantidad Productos\n";

            foreach (var item in report)
            {
                csv += $"\"{item.CategoryName}\",\"{item.BrandName}\",\"{item.StoreName}\",{item.TotalUnits},{item.TotalPurchaseValue:F2},{item.TotalSaleValue:F2},{item.PotentialProfit:F2},{item.ProductCount}\n";
            }

            return csv;
        }

        private async Task<string> GenerateSalesPeriodCsv(ReportFiltersDto filters)
        {
            var report = await GetSalesPeriodReportAsync(filters);

            var csv = "Fecha,Ventas Totales,Transacciones,Ganancia,Items Vendidos\n";

            foreach (var day in report.DailySales)
            {
                csv += $"{day.Date:dd/MM/yyyy},{day.TotalSales:F2},{day.TransactionCount},{day.TotalProfit:F2},\n";
            }

            return csv;
        }

        private async Task<string> GenerateSimplePdfContentAsync(string reportType, ReportFiltersDto filters)
        {
            var content = await GenerateReportContentAsync(reportType, filters);

            var htmlContent = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Reporte {reportType}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; font-size: 12px; }}
        table {{ border-collapse: collapse; width: 100%; margin-top: 20px; }}
        th, td {{ border: 1px solid #333; padding: 6px; text-align: left; font-size: 11px; }}
        th {{ background-color: #f0f0f0; font-weight: bold; }}
        .header {{ color: #000; font-size: 18px; margin-bottom: 15px; font-weight: bold; }}
        .summary {{ background-color: #f9f9f9; padding: 10px; border: 1px solid #ccc; margin-bottom: 15px; }}
        .summary strong {{ font-weight: bold; }}
    </style>
</head>
<body>
    <div class='header'>Sistema de Inventario - Reporte {reportType}</div>
    <div class='summary'>
        <strong>Fecha de generación:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}<br>
        <strong>Filtros aplicados:</strong><br>
        - Fecha inicio: {filters.StartDate?.ToString("dd/MM/yyyy") ?? "N/A"}<br>
        - Fecha fin: {filters.EndDate?.ToString("dd/MM/yyyy") ?? "N/A"}<br>
        - Tienda: {filters.StoreCode ?? "Todas"}
    </div>
    {content}
</body>
</html>";
            return htmlContent;
        }

        private async Task AddStockCriticalPdfContent(Document document, ReportFiltersDto filters)
        {
            var report = await GetStockCriticalReportAsync(filters);

            // Summary section
            document.Add(new Paragraph("Resumen de Stock Crítico").SetFontSize(14).SetMarginTop(10));
            document.Add(new Paragraph($"Productos con stock bajo: {report.TotalLowStockItems}"));
            document.Add(new Paragraph($"Productos sin stock: {report.TotalOutOfStockItems}"));
            document.Add(new Paragraph($"Valor en riesgo: {CurrencyHelper.FormatCurrency(report.TotalLowStockValue)}"));

            // Low stock table
            if (report.LowStockItems.Any())
            {
                document.Add(new Paragraph("Productos con Stock Bajo").SetFontSize(12).SetMarginTop(15));
                var table = new Table(6).UseAllAvailableWidth();

                // Headers
                table.AddHeaderCell("#");
                table.AddHeaderCell("Código");
                table.AddHeaderCell("Producto");
                table.AddHeaderCell("Stock Actual");
                table.AddHeaderCell("Stock Mínimo");
                table.AddHeaderCell("Tienda");

                // Data rows
                int rowNumber = 1;
                foreach (var item in report.LowStockItems)
                {
                    table.AddCell(rowNumber.ToString());
                    table.AddCell(item.ProductCode);
                    table.AddCell(item.ProductName);
                    table.AddCell(item.CurrentStock.ToString());
                    table.AddCell(item.MinimumStock.ToString());
                    table.AddCell(item.StoreName);
                    rowNumber++;
                }

                document.Add(table);
            }

            // Out of stock table
            if (report.OutOfStockItems.Any())
            {
                document.Add(new Paragraph("Productos sin Stock").SetFontSize(12).SetMarginTop(15));
                var table = new Table(6).UseAllAvailableWidth();

                // Headers
                table.AddHeaderCell("#");
                table.AddHeaderCell("Código");
                table.AddHeaderCell("Producto");
                table.AddHeaderCell("Stock Actual");
                table.AddHeaderCell("Stock Mínimo");
                table.AddHeaderCell("Tienda");

                // Data rows
                int rowNumber = 1;
                foreach (var item in report.OutOfStockItems)
                {
                    table.AddCell(rowNumber.ToString());
                    table.AddCell(item.ProductCode);
                    table.AddCell(item.ProductName);
                    table.AddCell(item.CurrentStock.ToString());
                    table.AddCell(item.MinimumStock.ToString());
                    table.AddCell(item.StoreName);
                    rowNumber++;
                }

                document.Add(table);
            }
        }

        private async Task AddInventoryValuationPdfContent(Document document, ReportFiltersDto filters)
        {
            var report = await GetInventoryValuationReportAsync(filters);

            var totalValue = report.Sum(r => r.TotalPurchaseValue);
            var totalProfit = report.Sum(r => r.PotentialProfit);

            // Summary section
            document.Add(new Paragraph("Valorización de Inventario").SetFontSize(14).SetMarginTop(10));
            document.Add(new Paragraph($"Valor total inventario: {CurrencyHelper.FormatCurrency(totalValue)}"));
            document.Add(new Paragraph($"Ganancia potencial: {CurrencyHelper.FormatCurrency(totalProfit)}"));
            document.Add(new Paragraph($"Categorías analizadas: {report.Count}"));

            // Valuation table
            if (report.Any())
            {
                document.Add(new Paragraph("Valorización por Categoría").SetFontSize(12).SetMarginTop(15));
                var table = new Table(5).UseAllAvailableWidth();

                // Headers
                table.AddHeaderCell("#");
                table.AddHeaderCell("Categoría");
                table.AddHeaderCell("Valor Compra");
                table.AddHeaderCell("Ganancia Potencial");
                table.AddHeaderCell("Productos");

                // Data rows
                int rowNumber = 1;
                foreach (var item in report)
                {
                    table.AddCell(rowNumber.ToString());
                    table.AddCell(item.CategoryName);
                    table.AddCell(CurrencyHelper.FormatCurrency(item.TotalPurchaseValue));
                    table.AddCell(CurrencyHelper.FormatCurrency(item.PotentialProfit));
                    table.AddCell(item.ProductCount.ToString());
                    rowNumber++;
                }

                document.Add(table);
            }
        }

        private async Task AddSalesPeriodPdfContent(Document document, ReportFiltersDto filters)
        {
            var report = await GetSalesPeriodReportAsync(filters);

            // Summary section
            document.Add(new Paragraph("Reporte de Ventas por Período").SetFontSize(14).SetMarginTop(10));
            document.Add(new Paragraph($"Ventas totales: {CurrencyHelper.FormatCurrency(report.TotalSales)}"));
            document.Add(new Paragraph($"Total transacciones: {report.TotalTransactions}"));
            document.Add(new Paragraph($"Ticket promedio: {CurrencyHelper.FormatCurrency(report.AverageTicket)}"));
            document.Add(new Paragraph($"Ganancia total: {CurrencyHelper.FormatCurrency(report.TotalProfit)}"));
            document.Add(new Paragraph($"Total items vendidos: {report.TotalItems}"));

            // Daily sales table
            if (report.DailySales.Any())
            {
                document.Add(new Paragraph("Ventas por Día").SetFontSize(12).SetMarginTop(15));
                var table = new Table(5).UseAllAvailableWidth();

                // Headers
                table.AddHeaderCell("#");
                table.AddHeaderCell("Fecha");
                table.AddHeaderCell("Ventas");
                table.AddHeaderCell("Transacciones");
                table.AddHeaderCell("Ganancia");

                // Data rows
                int rowNumber = 1;
                foreach (var day in report.DailySales)
                {
                    table.AddCell(rowNumber.ToString());
                    table.AddCell(day.Date.ToString("dd/MM/yyyy"));
                    table.AddCell(CurrencyHelper.FormatCurrency(day.TotalSales));
                    table.AddCell(day.TransactionCount.ToString());
                    table.AddCell(CurrencyHelper.FormatCurrency(day.TotalProfit));
                    rowNumber++;
                }

                document.Add(table);
            }
        }

        private async Task AddStockCriticalExcelContent(XLWorkbook workbook, ReportFiltersDto filters)
        {
            var report = await GetStockCriticalReportAsync(filters);
            var worksheet = workbook.Worksheets.Add("Stock Crítico");

            // Title and summary
            worksheet.Cell(1, 1).Value = "Reporte de Stock Crítico";
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Cell(1, 1).Style.Font.Bold = true;

            worksheet.Cell(3, 1).Value = $"Fecha de generación: {DateTime.Now:dd/MM/yyyy HH:mm}";
            worksheet.Cell(4, 1).Value = $"Productos con stock bajo: {report.TotalLowStockItems}";
            worksheet.Cell(5, 1).Value = $"Productos sin stock: {report.TotalOutOfStockItems}";
            worksheet.Cell(6, 1).Value = $"Valor en riesgo: {CurrencyHelper.FormatCurrency(report.TotalLowStockValue)}";

            int currentRow = 8;

            // Low stock items
            if (report.LowStockItems.Any())
            {
                worksheet.Cell(currentRow, 1).Value = "Productos con Stock Bajo";
                worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                currentRow += 2;

                // Headers
                worksheet.Cell(currentRow, 1).Value = "Código";
                worksheet.Cell(currentRow, 2).Value = "Producto";
                worksheet.Cell(currentRow, 3).Value = "Categoría";
                worksheet.Cell(currentRow, 4).Value = "Marca";
                worksheet.Cell(currentRow, 5).Value = "Tienda";
                worksheet.Cell(currentRow, 6).Value = "Stock Actual";
                worksheet.Cell(currentRow, 7).Value = "Stock Mínimo";

                var headerRange = worksheet.Range(currentRow, 1, currentRow, 7);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                currentRow++;

                // Data
                foreach (var item in report.LowStockItems)
                {
                    worksheet.Cell(currentRow, 1).Value = item.ProductCode;
                    worksheet.Cell(currentRow, 2).Value = item.ProductName;
                    worksheet.Cell(currentRow, 3).Value = item.CategoryName;
                    worksheet.Cell(currentRow, 4).Value = item.BrandName;
                    worksheet.Cell(currentRow, 5).Value = item.StoreName;
                    worksheet.Cell(currentRow, 6).Value = item.CurrentStock;
                    worksheet.Cell(currentRow, 7).Value = item.MinimumStock;
                    currentRow++;
                }
                currentRow += 2;
            }

            // Out of stock items
            if (report.OutOfStockItems.Any())
            {
                worksheet.Cell(currentRow, 1).Value = "Productos sin Stock";
                worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                currentRow += 2;

                // Headers
                worksheet.Cell(currentRow, 1).Value = "Código";
                worksheet.Cell(currentRow, 2).Value = "Producto";
                worksheet.Cell(currentRow, 3).Value = "Categoría";
                worksheet.Cell(currentRow, 4).Value = "Marca";
                worksheet.Cell(currentRow, 5).Value = "Tienda";
                worksheet.Cell(currentRow, 6).Value = "Stock Actual";
                worksheet.Cell(currentRow, 7).Value = "Stock Mínimo";

                var headerRange = worksheet.Range(currentRow, 1, currentRow, 7);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                currentRow++;

                // Data
                foreach (var item in report.OutOfStockItems)
                {
                    worksheet.Cell(currentRow, 1).Value = item.ProductCode;
                    worksheet.Cell(currentRow, 2).Value = item.ProductName;
                    worksheet.Cell(currentRow, 3).Value = item.CategoryName;
                    worksheet.Cell(currentRow, 4).Value = item.BrandName;
                    worksheet.Cell(currentRow, 5).Value = item.StoreName;
                    worksheet.Cell(currentRow, 6).Value = item.CurrentStock;
                    worksheet.Cell(currentRow, 7).Value = item.MinimumStock;
                    currentRow++;
                }
            }

            worksheet.Columns().AdjustToContents();
        }

        private async Task AddInventoryValuationExcelContent(XLWorkbook workbook, ReportFiltersDto filters)
        {
            var report = await GetInventoryValuationReportAsync(filters);
            var worksheet = workbook.Worksheets.Add("Valorización");

            var totalValue = report.Sum(r => r.TotalPurchaseValue);
            var totalProfit = report.Sum(r => r.PotentialProfit);

            // Title and summary
            worksheet.Cell(1, 1).Value = "Reporte de Valorización de Inventario";
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Cell(1, 1).Style.Font.Bold = true;

            worksheet.Cell(3, 1).Value = $"Fecha de generación: {DateTime.Now:dd/MM/yyyy HH:mm}";
            worksheet.Cell(4, 1).Value = $"Valor total inventario: {CurrencyHelper.FormatCurrency(totalValue)}";
            worksheet.Cell(5, 1).Value = $"Ganancia potencial: {CurrencyHelper.FormatCurrency(totalProfit)}";
            worksheet.Cell(6, 1).Value = $"Categorías analizadas: {report.Count}";

            if (report.Any())
            {
                int currentRow = 8;
                worksheet.Cell(currentRow, 1).Value = "Valorización por Categoría";
                worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                currentRow += 2;

                // Headers
                worksheet.Cell(currentRow, 1).Value = "Categoría";
                worksheet.Cell(currentRow, 2).Value = "Marca";
                worksheet.Cell(currentRow, 3).Value = "Tienda";
                worksheet.Cell(currentRow, 4).Value = "Total Unidades";
                worksheet.Cell(currentRow, 5).Value = "Valor Compra";
                worksheet.Cell(currentRow, 6).Value = "Valor Venta";
                worksheet.Cell(currentRow, 7).Value = "Ganancia Potencial";
                worksheet.Cell(currentRow, 8).Value = "Cantidad Productos";

                var headerRange = worksheet.Range(currentRow, 1, currentRow, 8);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                currentRow++;

                // Data
                foreach (var item in report)
                {
                    worksheet.Cell(currentRow, 1).Value = item.CategoryName;
                    worksheet.Cell(currentRow, 2).Value = item.BrandName;
                    worksheet.Cell(currentRow, 3).Value = item.StoreName;
                    worksheet.Cell(currentRow, 4).Value = item.TotalUnits;
                    worksheet.Cell(currentRow, 5).Value = item.TotalPurchaseValue;
                    worksheet.Cell(currentRow, 6).Value = item.TotalSaleValue;
                    worksheet.Cell(currentRow, 7).Value = item.PotentialProfit;
                    worksheet.Cell(currentRow, 8).Value = item.ProductCount;
                    currentRow++;
                }

                // Format currency columns
                worksheet.Range(currentRow - report.Count, 5, currentRow - 1, 7).Style.NumberFormat.Format = "#,##0.00";
            }

            worksheet.Columns().AdjustToContents();
        }

        private async Task AddSalesPeriodExcelContent(XLWorkbook workbook, ReportFiltersDto filters)
        {
            var report = await GetSalesPeriodReportAsync(filters);
            var worksheet = workbook.Worksheets.Add("Ventas por Período");

            // Title and summary
            worksheet.Cell(1, 1).Value = "Reporte de Ventas por Período";
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Cell(1, 1).Style.Font.Bold = true;

            worksheet.Cell(3, 1).Value = $"Fecha de generación: {DateTime.Now:dd/MM/yyyy HH:mm}";
            worksheet.Cell(4, 1).Value = $"Período: {report.StartDate:dd/MM/yyyy} - {report.EndDate:dd/MM/yyyy}";
            worksheet.Cell(5, 1).Value = $"Ventas totales: {CurrencyHelper.FormatCurrency(report.TotalSales)}";
            worksheet.Cell(6, 1).Value = $"Total transacciones: {report.TotalTransactions}";
            worksheet.Cell(7, 1).Value = $"Ticket promedio: {CurrencyHelper.FormatCurrency(report.AverageTicket)}";
            worksheet.Cell(8, 1).Value = $"Ganancia total: {CurrencyHelper.FormatCurrency(report.TotalProfit)}";
            worksheet.Cell(9, 1).Value = $"Total items vendidos: {report.TotalItems}";

            if (report.DailySales.Any())
            {
                int currentRow = 11;
                worksheet.Cell(currentRow, 1).Value = "Ventas por Día";
                worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                currentRow += 2;

                // Headers
                worksheet.Cell(currentRow, 1).Value = "Fecha";
                worksheet.Cell(currentRow, 2).Value = "Ventas Totales";
                worksheet.Cell(currentRow, 3).Value = "Transacciones";
                worksheet.Cell(currentRow, 4).Value = "Ganancia";

                var headerRange = worksheet.Range(currentRow, 1, currentRow, 4);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                currentRow++;

                // Data
                foreach (var day in report.DailySales)
                {
                    worksheet.Cell(currentRow, 1).Value = day.Date;
                    worksheet.Cell(currentRow, 2).Value = day.TotalSales;
                    worksheet.Cell(currentRow, 3).Value = day.TransactionCount;
                    worksheet.Cell(currentRow, 4).Value = day.TotalProfit;
                    currentRow++;
                }

                // Format date and currency columns
                worksheet.Range(currentRow - report.DailySales.Count, 1, currentRow - 1, 1).Style.NumberFormat.Format = "dd/mm/yyyy";
                worksheet.Range(currentRow - report.DailySales.Count, 2, currentRow - 1, 2).Style.NumberFormat.Format = "#,##0.00";
                worksheet.Range(currentRow - report.DailySales.Count, 4, currentRow - 1, 4).Style.NumberFormat.Format = "#,##0.00";
            }

            worksheet.Columns().AdjustToContents();
        }

        private async Task AddTopProductsExcelContent(XLWorkbook workbook, ReportFiltersDto filters)
        {
            var report = await GetTopProductsReportAsync(filters, 20);
            var worksheet = workbook.Worksheets.Add("Top Productos");

            worksheet.Cell(1, 1).Value = "Reporte de Top Productos";
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Cell(1, 1).Style.Font.Bold = true;

            worksheet.Cell(3, 1).Value = $"Fecha de generación: {DateTime.Now:dd/MM/yyyy HH:mm}";

            if (report.Any())
            {
                int currentRow = 5;

                // Headers
                worksheet.Cell(currentRow, 1).Value = "Código";
                worksheet.Cell(currentRow, 2).Value = "Producto";
                worksheet.Cell(currentRow, 3).Value = "Cantidad Vendida";
                worksheet.Cell(currentRow, 4).Value = "Ingresos Totales";
                worksheet.Cell(currentRow, 5).Value = "Ganancia Total";
                worksheet.Cell(currentRow, 6).Value = "Transacciones";

                var headerRange = worksheet.Range(currentRow, 1, currentRow, 6);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                currentRow++;

                // Data
                foreach (var product in report)
                {
                    worksheet.Cell(currentRow, 1).Value = product.ProductCode;
                    worksheet.Cell(currentRow, 2).Value = product.ProductName;
                    worksheet.Cell(currentRow, 3).Value = product.QuantitySold;
                    worksheet.Cell(currentRow, 4).Value = product.TotalRevenue;
                    worksheet.Cell(currentRow, 5).Value = product.TotalProfit;
                    worksheet.Cell(currentRow, 6).Value = product.TransactionCount;
                    currentRow++;
                }

                worksheet.Range(currentRow - report.Count, 4, currentRow - 1, 5).Style.NumberFormat.Format = "#,##0.00";
            }

            worksheet.Columns().AdjustToContents();
        }

        private async Task AddSalesByCategoryExcelContent(XLWorkbook workbook, ReportFiltersDto filters)
        {
            var report = await GetSalesByCategoryReportAsync(filters);
            var worksheet = workbook.Worksheets.Add("Ventas por Categoría");

            worksheet.Cell(1, 1).Value = "Reporte de Ventas por Categoría";
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Cell(1, 1).Style.Font.Bold = true;

            worksheet.Cell(3, 1).Value = $"Fecha de generación: {DateTime.Now:dd/MM/yyyy HH:mm}";

            if (report.Any())
            {
                int currentRow = 5;

                // Headers
                worksheet.Cell(currentRow, 1).Value = "Categoría";
                worksheet.Cell(currentRow, 2).Value = "Ventas Totales";
                worksheet.Cell(currentRow, 3).Value = "Ganancia Total";
                worksheet.Cell(currentRow, 4).Value = "Cantidad Vendida";
                worksheet.Cell(currentRow, 5).Value = "Productos Diferentes";

                var headerRange = worksheet.Range(currentRow, 1, currentRow, 5);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                currentRow++;

                // Data
                foreach (var category in report)
                {
                    worksheet.Cell(currentRow, 1).Value = category.CategoryName;
                    worksheet.Cell(currentRow, 2).Value = category.TotalSales;
                    worksheet.Cell(currentRow, 3).Value = category.TotalProfit;
                    worksheet.Cell(currentRow, 4).Value = category.QuantitySold;
                    worksheet.Cell(currentRow, 5).Value = category.ProductCount;
                    currentRow++;
                }

                worksheet.Range(currentRow - report.Count, 2, currentRow - 1, 3).Style.NumberFormat.Format = "#,##0.00";
            }

            worksheet.Columns().AdjustToContents();
        }

        private async Task AddCustomerAnalysisExcelContent(XLWorkbook workbook, ReportFiltersDto filters)
        {
            var report = await GetCustomerAnalysisReportAsync(filters);
            var worksheet = workbook.Worksheets.Add("Análisis de Clientes");

            worksheet.Cell(1, 1).Value = "Reporte de Análisis de Clientes";
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Cell(1, 1).Style.Font.Bold = true;

            worksheet.Cell(3, 1).Value = $"Fecha de generación: {DateTime.Now:dd/MM/yyyy HH:mm}";

            if (report.Any())
            {
                int currentRow = 5;

                // Headers
                worksheet.Cell(currentRow, 1).Value = "Cliente";
                worksheet.Cell(currentRow, 2).Value = "Email";
                worksheet.Cell(currentRow, 3).Value = "Compras Totales";
                worksheet.Cell(currentRow, 4).Value = "Transacciones";
                worksheet.Cell(currentRow, 5).Value = "Ticket Promedio";
                worksheet.Cell(currentRow, 6).Value = "Primera Compra";
                worksheet.Cell(currentRow, 7).Value = "Última Compra";
                worksheet.Cell(currentRow, 8).Value = "Días sin Comprar";

                var headerRange = worksheet.Range(currentRow, 1, currentRow, 8);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                currentRow++;

                // Data
                foreach (var customer in report)
                {
                    worksheet.Cell(currentRow, 1).Value = customer.CustomerName;
                    worksheet.Cell(currentRow, 2).Value = customer.Email;
                    worksheet.Cell(currentRow, 3).Value = customer.TotalPurchases;
                    worksheet.Cell(currentRow, 4).Value = customer.TransactionCount;
                    worksheet.Cell(currentRow, 5).Value = customer.AverageTicket;
                    worksheet.Cell(currentRow, 6).Value = customer.FirstPurchase;
                    worksheet.Cell(currentRow, 7).Value = customer.LastPurchase;
                    worksheet.Cell(currentRow, 8).Value = customer.DaysSinceLastPurchase;
                    currentRow++;
                }

                worksheet.Range(currentRow - report.Count, 3, currentRow - 1, 5).Style.NumberFormat.Format = "#,##0.00";
                worksheet.Range(currentRow - report.Count, 6, currentRow - 1, 7).Style.NumberFormat.Format = "dd/mm/yyyy";
            }

            worksheet.Columns().AdjustToContents();
        }

        private async Task AddStorePerformanceExcelContent(XLWorkbook workbook, ReportFiltersDto filters)
        {
            var report = await GetStorePerformanceReportAsync(filters);
            var worksheet = workbook.Worksheets.Add("Performance por Tienda");

            worksheet.Cell(1, 1).Value = "Reporte de Performance por Tienda";
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Cell(1, 1).Style.Font.Bold = true;

            worksheet.Cell(3, 1).Value = $"Fecha de generación: {DateTime.Now:dd/MM/yyyy HH:mm}";

            if (report.Any())
            {
                int currentRow = 5;

                // Headers
                worksheet.Cell(currentRow, 1).Value = "Código";
                worksheet.Cell(currentRow, 2).Value = "Tienda";
                worksheet.Cell(currentRow, 3).Value = "Ventas Totales";
                worksheet.Cell(currentRow, 4).Value = "Ganancia Total";
                worksheet.Cell(currentRow, 5).Value = "Margen %";
                worksheet.Cell(currentRow, 6).Value = "Transacciones";
                worksheet.Cell(currentRow, 7).Value = "Ticket Promedio";
                worksheet.Cell(currentRow, 8).Value = "Valor Inventario";

                var headerRange = worksheet.Range(currentRow, 1, currentRow, 8);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                currentRow++;

                // Data
                foreach (var store in report)
                {
                    worksheet.Cell(currentRow, 1).Value = store.StoreCode;
                    worksheet.Cell(currentRow, 2).Value = store.StoreName;
                    worksheet.Cell(currentRow, 3).Value = store.TotalSales;
                    worksheet.Cell(currentRow, 4).Value = store.TotalProfit;
                    worksheet.Cell(currentRow, 5).Value = store.ProfitMargin;
                    worksheet.Cell(currentRow, 6).Value = store.TransactionCount;
                    worksheet.Cell(currentRow, 7).Value = store.AverageTicket;
                    worksheet.Cell(currentRow, 8).Value = store.InventoryValue;
                    currentRow++;
                }

                worksheet.Range(currentRow - report.Count, 3, currentRow - 1, 4).Style.NumberFormat.Format = "#,##0.00";
                worksheet.Range(currentRow - report.Count, 7, currentRow - 1, 8).Style.NumberFormat.Format = "#,##0.00";
            }

            worksheet.Columns().AdjustToContents();
        }

        private async Task AddProductsWithoutMovementExcelContent(XLWorkbook workbook, ReportFiltersDto filters)
        {
            var report = await GetProductsWithoutMovementReportAsync(filters);
            var worksheet = workbook.Worksheets.Add("Productos sin Movimiento");

            worksheet.Cell(1, 1).Value = "Reporte de Productos sin Movimiento";
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Cell(1, 1).Style.Font.Bold = true;

            worksheet.Cell(3, 1).Value = $"Fecha de generación: {DateTime.Now:dd/MM/yyyy HH:mm}";
            worksheet.Cell(4, 1).Value = $"Productos sin movimiento ({filters.DaysThreshold ?? 30} días): {report.Count}";

            if (report.Any())
            {
                int currentRow = 6;

                // Headers
                worksheet.Cell(currentRow, 1).Value = "Código";
                worksheet.Cell(currentRow, 2).Value = "Producto";
                worksheet.Cell(currentRow, 3).Value = "Categoría";
                worksheet.Cell(currentRow, 4).Value = "Marca";
                worksheet.Cell(currentRow, 5).Value = "Tienda";
                worksheet.Cell(currentRow, 6).Value = "Stock Actual";
                worksheet.Cell(currentRow, 7).Value = "Valor Total";

                var headerRange = worksheet.Range(currentRow, 1, currentRow, 7);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                currentRow++;

                // Data
                foreach (var product in report)
                {
                    worksheet.Cell(currentRow, 1).Value = product.ProductCode;
                    worksheet.Cell(currentRow, 2).Value = product.ProductName;
                    worksheet.Cell(currentRow, 3).Value = product.CategoryName;
                    worksheet.Cell(currentRow, 4).Value = product.BrandName;
                    worksheet.Cell(currentRow, 5).Value = product.StoreName;
                    worksheet.Cell(currentRow, 6).Value = product.CurrentStock;
                    worksheet.Cell(currentRow, 7).Value = product.TotalValue;
                    currentRow++;
                }

                worksheet.Range(currentRow - report.Count, 7, currentRow - 1, 7).Style.NumberFormat.Format = "#,##0.00";
            }

            worksheet.Columns().AdjustToContents();
        }

        // PDF Content Methods for new reports
        private async Task AddTopProductsPdfContent(Document document, ReportFiltersDto filters)
        {
            var report = await GetTopProductsReportAsync(filters, 10);

            document.Add(new Paragraph("Top Productos Más Vendidos").SetFontSize(14).SetMarginTop(10));

            if (report.Any())
            {
                var table = new Table(5).UseAllAvailableWidth();
                table.AddHeaderCell("#");
                table.AddHeaderCell("Producto");
                table.AddHeaderCell("Cantidad Vendida");
                table.AddHeaderCell("Ingresos");
                table.AddHeaderCell("Ganancia");

                int rowNumber = 1;
                foreach (var product in report)
                {
                    table.AddCell(rowNumber.ToString());
                    table.AddCell(product.ProductName);
                    table.AddCell(product.QuantitySold.ToString());
                    table.AddCell(CurrencyHelper.FormatCurrency(product.TotalRevenue));
                    table.AddCell(CurrencyHelper.FormatCurrency(product.TotalProfit));
                    rowNumber++;
                }

                document.Add(table);
            }
        }

        private async Task AddSalesByCategoryPdfContent(Document document, ReportFiltersDto filters)
        {
            var report = await GetSalesByCategoryReportAsync(filters);

            document.Add(new Paragraph("Ventas por Categoría").SetFontSize(14).SetMarginTop(10));

            if (report.Any())
            {
                var table = new Table(5).UseAllAvailableWidth();
                table.AddHeaderCell("#");
                table.AddHeaderCell("Categoría");
                table.AddHeaderCell("Ventas Totales");
                table.AddHeaderCell("Ganancia");
                table.AddHeaderCell("Cantidad Vendida");

                int rowNumber = 1;
                foreach (var category in report)
                {
                    table.AddCell(rowNumber.ToString());
                    table.AddCell(category.CategoryName);
                    table.AddCell(CurrencyHelper.FormatCurrency(category.TotalSales));
                    table.AddCell(CurrencyHelper.FormatCurrency(category.TotalProfit));
                    table.AddCell(category.QuantitySold.ToString());
                    rowNumber++;
                }

                document.Add(table);
            }
        }

        private async Task AddCustomerAnalysisPdfContent(Document document, ReportFiltersDto filters)
        {
            var report = await GetCustomerAnalysisReportAsync(filters);

            document.Add(new Paragraph("Análisis de Clientes").SetFontSize(14).SetMarginTop(10));

            if (report.Any())
            {
                var table = new Table(6).UseAllAvailableWidth();
                table.AddHeaderCell("#");
                table.AddHeaderCell("Cliente");
                table.AddHeaderCell("Compras Totales");
                table.AddHeaderCell("Transacciones");
                table.AddHeaderCell("Ticket Promedio");
                table.AddHeaderCell("Días sin Comprar");

                int rowNumber = 1;
                foreach (var customer in report.Take(15))
                {
                    table.AddCell(rowNumber.ToString());
                    table.AddCell(customer.CustomerName);
                    table.AddCell(CurrencyHelper.FormatCurrency(customer.TotalPurchases));
                    table.AddCell(customer.TransactionCount.ToString());
                    table.AddCell(CurrencyHelper.FormatCurrency(customer.AverageTicket));
                    table.AddCell(customer.DaysSinceLastPurchase.ToString());
                    rowNumber++;
                }

                document.Add(table);
            }
        }

        private async Task AddStorePerformancePdfContent(Document document, ReportFiltersDto filters)
        {
            var report = await GetStorePerformanceReportAsync(filters);

            document.Add(new Paragraph("Performance por Tienda").SetFontSize(14).SetMarginTop(10));

            if (report.Any())
            {
                var table = new Table(6).UseAllAvailableWidth();
                table.AddHeaderCell("#");
                table.AddHeaderCell("Tienda");
                table.AddHeaderCell("Ventas Totales");
                table.AddHeaderCell("Ganancia");
                table.AddHeaderCell("Margen %");
                table.AddHeaderCell("Transacciones");

                int rowNumber = 1;
                foreach (var store in report)
                {
                    table.AddCell(rowNumber.ToString());
                    table.AddCell(store.StoreName);
                    table.AddCell(CurrencyHelper.FormatCurrency(store.TotalSales));
                    table.AddCell(CurrencyHelper.FormatCurrency(store.TotalProfit));
                    table.AddCell($"{store.ProfitMargin:F1}%");
                    table.AddCell(store.TransactionCount.ToString());
                    rowNumber++;
                }

                document.Add(table);
            }
        }

        private async Task AddProductsWithoutMovementPdfContent(Document document, ReportFiltersDto filters)
        {
            var report = await GetProductsWithoutMovementReportAsync(filters);

            document.Add(new Paragraph($"Productos sin Movimiento ({filters.DaysThreshold ?? 30} días)").SetFontSize(14).SetMarginTop(10));
            document.Add(new Paragraph($"Total productos sin movimiento: {report.Count}"));

            if (report.Any())
            {
                var table = new Table(6).UseAllAvailableWidth();
                table.AddHeaderCell("#");
                table.AddHeaderCell("Código");
                table.AddHeaderCell("Producto");
                table.AddHeaderCell("Categoría");
                table.AddHeaderCell("Stock Actual");
                table.AddHeaderCell("Valor Total");

                int rowNumber = 1;
                foreach (var product in report.Take(20))
                {
                    table.AddCell(rowNumber.ToString());
                    table.AddCell(product.ProductCode);
                    table.AddCell(product.ProductName);
                    table.AddCell(product.CategoryName);
                    table.AddCell(product.CurrentStock.ToString());
                    table.AddCell(CurrencyHelper.FormatCurrency(product.TotalValue));
                    rowNumber++;
                }

                document.Add(table);
            }
        }
    }
}