using System.Threading.Tasks;
using InventorySystem.Application.DTOs.Reports;

namespace InventorySystem.Application.Interfaces
{
    public interface IReportsService
    {
        // Inventory Reports
        Task<StockCriticalReportDto> GetStockCriticalReportAsync(ReportFiltersDto filters);
        Task<List<InventoryValuationReportDto>> GetInventoryValuationReportAsync(ReportFiltersDto filters);
        Task<List<ProductMovementReportDto>> GetProductMovementReportAsync(ReportFiltersDto filters);
        Task<List<InventoryReportDto>> GetProductsWithoutMovementReportAsync(ReportFiltersDto filters);

        // Sales Reports
        Task<SalesPeriodReportDto> GetSalesPeriodReportAsync(ReportFiltersDto filters);
        Task<List<TopProductDto>> GetTopProductsReportAsync(ReportFiltersDto filters, int topCount = 10);
        Task<List<CategorySalesDto>> GetSalesByCategoryReportAsync(ReportFiltersDto filters);
        Task<List<CustomerAnalysisDto>> GetCustomerAnalysisReportAsync(ReportFiltersDto filters);
        Task<List<SalesReportDto>> GetEmployeeSalesReportAsync(ReportFiltersDto filters);

        // Store Performance Reports
        Task<List<StorePerformanceDto>> GetStorePerformanceReportAsync(ReportFiltersDto filters);
        Task<List<InventoryReportDto>> GetStoreStockReportAsync(ReportFiltersDto filters);

        // Export Functionality
        Task<byte[]> ExportReportAsync(string reportType, ReportFiltersDto filters, ReportExportOptionsDto exportOptions);
    }

    public class StorePerformanceDto
    {
        public string StoreCode { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
        public decimal TotalSales { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal ProfitMargin { get; set; }
        public int TransactionCount { get; set; }
        public decimal AverageTicket { get; set; }
        public int ProductCount { get; set; }
        public decimal InventoryValue { get; set; }
        public int EmployeeCount { get; set; }
        public decimal SalesPerEmployee { get; set; }
    }
}