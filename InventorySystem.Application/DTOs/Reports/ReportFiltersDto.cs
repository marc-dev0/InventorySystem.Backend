using System;

namespace InventorySystem.Application.DTOs.Reports
{
    public class ReportFiltersDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? StoreCode { get; set; }
        public int? CategoryId { get; set; }
        public int? BrandId { get; set; }
        public int? CustomerId { get; set; }
        public int? EmployeeId { get; set; }
        public string? ProductCode { get; set; }
        public bool IncludeInactive { get; set; } = false;
        public int? DaysThreshold { get; set; }
        public decimal? MinimumAmount { get; set; }
        public decimal? MaximumAmount { get; set; }
    }

    public class ReportMetadataDto
    {
        public string ReportName { get; set; } = string.Empty;
        public DateTime GeneratedDate { get; set; }
        public string GeneratedBy { get; set; } = string.Empty;
        public ReportFiltersDto Filters { get; set; } = new();
        public int TotalRecords { get; set; }
        public TimeSpan ProcessingTime { get; set; }
    }

    public class ReportExportOptionsDto
    {
        public string Format { get; set; } = "PDF"; // PDF, Excel, CSV
        public bool IncludeCharts { get; set; } = true;
        public bool IncludeDetails { get; set; } = true;
        public string? Template { get; set; }
        public string? CompanyInfo { get; set; }
        public string? ReportTitle { get; set; }
    }
}