namespace InventorySystem.Application.DTOs;

public class TandiaProductDto
{
    public string Store { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Categories { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? Features { get; set; }
    public string? Taxes { get; set; }
    public decimal CostPrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Stock { get; set; }
    public int MinStock { get; set; }
    public string? Location { get; set; }
    public decimal SalePrice { get; set; }
    public string? Unit { get; set; }
    public string? PriceListName { get; set; }
    public decimal ConversionFactor { get; set; }
    public decimal WholesalePrice { get; set; }
    public int MinQuantity { get; set; }
    public int MaxQuantity { get; set; }
}

public class TandiaSaleDetailDto
{
    public string CompanyName { get; set; } = string.Empty;
    public string SalesEmployee { get; set; } = string.Empty;
    public string Warehouse { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerDocument { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public string? RelatedDocument { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan Time { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public decimal Quantity { get; set; }
    public decimal SalePrice { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public decimal DiscountApplied { get; set; }
    public decimal Conversion { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string? AlternativeCode { get; set; }
    public string? Brand { get; set; }
    public string? Category { get; set; }
    public string? Features { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Supplier { get; set; }
    public decimal CostPrice { get; set; }
    public string RegistrationEmployee { get; set; } = string.Empty;
}

public class BulkUploadResultDto
{
    public int TotalRecords { get; set; }
    public int SuccessCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public TimeSpan ProcessingTime { get; set; }
}

public class TandiaUploadSummaryDto
{
    public BulkUploadResultDto ProductsResult { get; set; } = new();
    public BulkUploadResultDto SalesResult { get; set; } = new();
    public string UploadedBy { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; }
}