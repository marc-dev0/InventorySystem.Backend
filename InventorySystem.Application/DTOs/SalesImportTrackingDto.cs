namespace InventorySystem.Application.DTOs;

public class SalesImportBatchDto
{
    public DateTime ImportedAt { get; set; }
    public string ImportSource { get; set; } = string.Empty;
    public int TotalSales { get; set; }
    public int TotalItems { get; set; }
    public decimal TotalAmount { get; set; }
    public List<string> UniqueProducts { get; set; } = new();
    public string SampleSaleNumber { get; set; } = string.Empty;
}

public class SalesDeleteResultDto
{
    public DateTime ImportedAt { get; set; }
    public string ImportSource { get; set; } = string.Empty;
    public int DeletedSales { get; set; }
    public int DeletedSaleDetails { get; set; }
    public int RevertedMovements { get; set; }
    public int AffectedProducts { get; set; }
    public List<string> UpdatedProductCodes { get; set; } = new();
    public decimal TotalRevertedAmount { get; set; }
    public string DeletedBy { get; set; } = string.Empty;
    public DateTime DeletedAt { get; set; }
}