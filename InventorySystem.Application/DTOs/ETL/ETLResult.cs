namespace InventorySystem.Application.DTOs.ETL;

public class ETLResult<T>
{
    public bool IsSuccess { get; set; }
    public int TotalRecords { get; set; }
    public int ProcessedRecords { get; set; }
    public int SkippedRecords { get; set; }
    public int ErrorRecords { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<T> Data { get; set; } = new();
    public TimeSpan ProcessingTime { get; set; }
    public string ImportBatchCode { get; set; } = string.Empty;
}

public class ProductImportDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal Stock { get; set; }
    public decimal MinimumStock { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Supplier { get; set; }
    public string? Branch { get; set; }
    public string? Store { get; set; }
    public bool Active { get; set; } = true;
}

public class StockImportDto
{
    public string ProductCode { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal MaximumStock { get; set; }
}

public class SaleImportDto
{
    public string DocumentNumber { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerDocument { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
}