namespace InventorySystem.Application.DTOs;

public class StockValidationResultDto
{
    public bool IsValid { get; set; }
    public int TotalProducts { get; set; }
    public int ProductsWithIssues { get; set; }
    public int CriticalIssues { get; set; }
    public int WarningIssues { get; set; }
    public List<StockIssueDto> Issues { get; set; } = new();
    public string ValidationSummary { get; set; } = string.Empty;
    public DateTime ValidationDate { get; set; } = DateTime.UtcNow;
}

public class StockIssueDto
{
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string StoreCode { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int RequestedQuantity { get; set; }
    public int ResultingStock { get; set; }
    public StockIssueType IssueType { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public int SaleLineNumber { get; set; } // Para identificar la línea en el Excel
    public string DocumentNumber { get; set; } = string.Empty;
}

public enum StockIssueType
{
    NegativeStock,          // Stock quedaría negativo
    InsufficientStock,      // Stock insuficiente para la venta
    LowStockWarning,        // Stock bajo después de la venta
    ProductNotFound,        // Producto no existe
    StoreNotFound,          // Tienda no existe
    ZeroStock               // Stock actual es 0
}

public class StockSimulationDto
{
    public string ProductCode { get; set; } = string.Empty;
    public string StoreCode { get; set; } = string.Empty;
    public int InitialStock { get; set; }
    public int TotalSalesQuantity { get; set; }
    public int FinalStock { get; set; }
    public List<SaleSimulationDto> Sales { get; set; } = new();
}

public class SaleSimulationDto
{
    public string DocumentNumber { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime SaleDate { get; set; }
    public int StockAfterSale { get; set; }
    public bool CausesNegativeStock { get; set; }
}