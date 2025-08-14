namespace InventorySystem.Application.DTOs;

public class ImportBatchDto
{
    public int Id { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public string BatchType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? StoreCode { get; set; }
    public int TotalRecords { get; set; }
    public int SuccessCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }
    public DateTime ImportDate { get; set; }
    public string ImportedBy { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public string? DeleteReason { get; set; }
}

public class BatchDeleteResultDto
{
    public int BatchId { get; set; }
    public string BatchType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int DeletedSales { get; set; }
    public int DeletedSaleDetails { get; set; }
    public int RevertedMovements { get; set; }
    public int AffectedProducts { get; set; }
    public List<string> ProductsUpdated { get; set; } = new();
    public string DeletedBy { get; set; } = string.Empty;
    public DateTime DeletedAt { get; set; }
}