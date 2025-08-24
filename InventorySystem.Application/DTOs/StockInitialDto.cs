namespace InventorySystem.Application.DTOs;

public class StockLoadResultDto
{
    public int TotalRecords { get; set; }
    public int SuccessCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public TimeSpan ProcessingTime { get; set; }
    
    // Campos específicos de stock
    public int ProcessedProducts { get; set; }
    public int SkippedProducts { get; set; }
    public decimal TotalStock { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string StoreCode { get; set; } = string.Empty;
}

public class StockClearResultDto
{
    public int ClearedProductStocks { get; set; }
    public int ClearedMovements { get; set; }
    public int ResetProducts { get; set; }
}

public class StoreClearResultDto
{
    public string StoreCode { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public int ClearedProductStocks { get; set; }
    public int ClearedMovements { get; set; }
}

public class StockSummaryDto
{
    public int TotalProducts { get; set; }
    public int ProductsWithStock { get; set; }
    public int ProductsWithoutStock { get; set; }
    public List<StoreStockSummaryDto> Stores { get; set; } = new();
}

public class StoreStockSummaryDto
{
    public int StoreId { get; set; }
    public string StoreCode { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public int ProductsWithStock { get; set; }
    public decimal TotalStock { get; set; }
    public int ProductsUnderMinimum { get; set; }
}

public class StockItemDto
{
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal MaximumStock { get; set; }
    public decimal AverageCost { get; set; }
}