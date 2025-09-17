using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InventorySystem.Application.DTOs.Reports;
using InventorySystem.Application.Interfaces;

namespace InventorySystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly IReportsService _reportsService;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(IReportsService reportsService, ILogger<ReportsController> logger)
        {
            _reportsService = reportsService;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene reporte de stock crítico (productos con stock bajo y sin stock)
        /// </summary>
        [HttpPost("stock-critical")]
        public async Task<ActionResult<StockCriticalReportDto>> GetStockCriticalReport(ReportFiltersDto filters)
        {
            try
            {
                _logger.LogInformation("Generating stock critical report with filters: {@Filters}", filters);

                var report = await _reportsService.GetStockCriticalReportAsync(filters);

                _logger.LogInformation("Stock critical report generated successfully. Low stock items: {LowStock}, Out of stock: {OutOfStock}",
                    report.TotalLowStockItems, report.TotalOutOfStockItems);

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating stock critical report");
                return StatusCode(500, new { message = "Error al generar el reporte de stock crítico" });
            }
        }

        /// <summary>
        /// Obtiene reporte de valorización de inventario
        /// </summary>
        [HttpPost("inventory-valuation")]
        public async Task<ActionResult<List<InventoryValuationReportDto>>> GetInventoryValuationReport(ReportFiltersDto filters)
        {
            try
            {
                _logger.LogInformation("Generating inventory valuation report with filters: {@Filters}", filters);

                var report = await _reportsService.GetInventoryValuationReportAsync(filters);

                _logger.LogInformation("Inventory valuation report generated successfully with {Count} records", report.Count);

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating inventory valuation report");
                return StatusCode(500, new { message = "Error al generar el reporte de valorización de inventario" });
            }
        }

        /// <summary>
        /// Obtiene reporte de ventas por período
        /// </summary>
        [HttpPost("sales-period")]
        public async Task<ActionResult<SalesPeriodReportDto>> GetSalesPeriodReport(ReportFiltersDto filters)
        {
            try
            {
                _logger.LogInformation("Generating sales period report with filters: {@Filters}", filters);

                var report = await _reportsService.GetSalesPeriodReportAsync(filters);

                _logger.LogInformation("Sales period report generated successfully. Total sales: {TotalSales}, Total transactions: {Transactions}",
                    report.TotalSales, report.TotalTransactions);

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating sales period report");
                return StatusCode(500, new { message = "Error al generar el reporte de ventas por período" });
            }
        }

        /// <summary>
        /// Obtiene reporte de movimientos de productos
        /// </summary>
        [HttpPost("product-movements")]
        public async Task<ActionResult<List<ProductMovementReportDto>>> GetProductMovementReport(ReportFiltersDto filters)
        {
            try
            {
                _logger.LogInformation("Generating product movement report with filters: {@Filters}", filters);

                var report = await _reportsService.GetProductMovementReportAsync(filters);

                _logger.LogInformation("Product movement report generated successfully with {Count} records", report.Count);

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating product movement report");
                return StatusCode(500, new { message = "Error al generar el reporte de movimientos de productos" });
            }
        }

        /// <summary>
        /// Obtiene reporte de productos sin movimiento
        /// </summary>
        [HttpPost("products-without-movement")]
        public async Task<ActionResult<List<InventoryReportDto>>> GetProductsWithoutMovementReport(ReportFiltersDto filters)
        {
            try
            {
                _logger.LogInformation("Generating products without movement report with filters: {@Filters}", filters);

                var report = await _reportsService.GetProductsWithoutMovementReportAsync(filters);

                _logger.LogInformation("Products without movement report generated successfully with {Count} records", report.Count);

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating products without movement report");
                return StatusCode(500, new { message = "Error al generar el reporte de productos sin movimiento" });
            }
        }

        /// <summary>
        /// Obtiene reporte de top productos más vendidos
        /// </summary>
        [HttpPost("top-products")]
        public async Task<ActionResult<List<TopProductDto>>> GetTopProductsReport(ReportFiltersDto filters, [FromQuery] int topCount = 10)
        {
            try
            {
                _logger.LogInformation("Generating top products report with filters: {@Filters}, topCount: {TopCount}", filters, topCount);

                var report = await _reportsService.GetTopProductsReportAsync(filters, topCount);

                _logger.LogInformation("Top products report generated successfully with {Count} records", report.Count);

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating top products report");
                return StatusCode(500, new { message = "Error al generar el reporte de top productos" });
            }
        }

        /// <summary>
        /// Obtiene reporte de ventas por categoría
        /// </summary>
        [HttpPost("sales-by-category")]
        public async Task<ActionResult<List<CategorySalesDto>>> GetSalesByCategoryReport(ReportFiltersDto filters)
        {
            try
            {
                _logger.LogInformation("Generating sales by category report with filters: {@Filters}", filters);

                var report = await _reportsService.GetSalesByCategoryReportAsync(filters);

                _logger.LogInformation("Sales by category report generated successfully with {Count} records", report.Count);

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating sales by category report");
                return StatusCode(500, new { message = "Error al generar el reporte de ventas por categoría" });
            }
        }

        /// <summary>
        /// Obtiene reporte de análisis de clientes
        /// </summary>
        [HttpPost("customer-analysis")]
        public async Task<ActionResult<List<CustomerAnalysisDto>>> GetCustomerAnalysisReport(ReportFiltersDto filters)
        {
            try
            {
                _logger.LogInformation("Generating customer analysis report with filters: {@Filters}", filters);

                var report = await _reportsService.GetCustomerAnalysisReportAsync(filters);

                _logger.LogInformation("Customer analysis report generated successfully with {Count} records", report.Count);

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating customer analysis report");
                return StatusCode(500, new { message = "Error al generar el reporte de análisis de clientes" });
            }
        }

        /// <summary>
        /// Obtiene reporte de performance por tienda
        /// </summary>
        [HttpPost("store-performance")]
        public async Task<ActionResult<List<StorePerformanceDto>>> GetStorePerformanceReport(ReportFiltersDto filters)
        {
            try
            {
                _logger.LogInformation("Generating store performance report with filters: {@Filters}", filters);

                var report = await _reportsService.GetStorePerformanceReportAsync(filters);

                _logger.LogInformation("Store performance report generated successfully with {Count} records", report.Count);

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating store performance report");
                return StatusCode(500, new { message = "Error al generar el reporte de performance por tienda" });
            }
        }

        /// <summary>
        /// Exporta un reporte en el formato especificado (PDF, Excel, CSV)
        /// </summary>
        [HttpPost("export/{reportType}")]
        public async Task<IActionResult> ExportReport(string reportType, ReportFiltersDto filters, [FromQuery] string format = "PDF", [FromQuery] bool includeCharts = false, [FromQuery] bool includeDetails = true)
        {
            try
            {
                var exportOptions = new ReportExportOptionsDto
                {
                    Format = format,
                    IncludeCharts = includeCharts,
                    IncludeDetails = includeDetails
                };

                _logger.LogInformation("Exporting report {ReportType} with filters: {@Filters}, options: {@Options}",
                    reportType, filters, exportOptions);

                var reportData = await _reportsService.ExportReportAsync(reportType, filters, exportOptions);

                var contentType = format.ToLower() switch
                {
                    "pdf" => "application/pdf",
                    "excel" or "xlsx" or "xls" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "csv" => "text/csv",
                    _ => "application/octet-stream"
                };

                var fileExtension = format.ToLower() switch
                {
                    "pdf" => "pdf",
                    "excel" or "xlsx" or "xls" => "xlsx",
                    "csv" => "csv",
                    _ => "txt"
                };

                var fileName = $"{reportType}_{DateTime.Now:yyyyMMdd_HHmmss}.{fileExtension}";

                _logger.LogInformation("Report {ReportType} exported successfully as {FileName}", reportType, fileName);

                return File(reportData, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting report {ReportType}", reportType);
                return StatusCode(500, new { message = $"Error al exportar el reporte {reportType}" });
            }
        }
    }
}