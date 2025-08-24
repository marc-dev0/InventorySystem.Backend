namespace InventorySystem.Core.Entities;

public class BackgroundJob : BaseEntity
{
    public string JobId { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty; // "SALES_IMPORT", "PRODUCTS_IMPORT", "STOCK_IMPORT"
    public string Status { get; set; } = string.Empty; // "PENDING", "PROCESSING", "COMPLETED", "FAILED"
    public string FileName { get; set; } = string.Empty;
    public string? StoreCode { get; set; }
    public int? ImportBatchId { get; set; }
    public int TotalRecords { get; set; }
    public int ProcessedRecords { get; set; }
    public int SuccessRecords { get; set; }
    public int ErrorRecords { get; set; }
    public int WarningRecords { get; set; }
    public decimal ProgressPercentage { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? WarningMessage { get; set; }
    public List<string> DetailedErrors { get; set; } = new List<string>();
    public List<string> DetailedWarnings { get; set; } = new List<string>();
    public string StartedBy { get; set; } = string.Empty;
    
    // Navigation properties
    public virtual ImportBatch? ImportBatch { get; set; }
}