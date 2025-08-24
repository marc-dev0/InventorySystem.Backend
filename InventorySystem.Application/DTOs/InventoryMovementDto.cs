using InventorySystem.Core.Entities;

namespace InventorySystem.Application.DTOs;

public class InventoryMovementDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public MovementType Type { get; set; }
    public string TypeDescription { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Reason { get; set; }
    public decimal PreviousStock { get; set; }
    public decimal NewStock { get; set; }
    public string? DocumentNumber { get; set; }
    public string? UserName { get; set; }
    public string? Source { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal? TotalCost { get; set; }
    
    // Product info
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    
    // Related documents
    public int? SaleId { get; set; }
    public string? SaleNumber { get; set; }
    public int? PurchaseId { get; set; }
    public string? PurchaseNumber { get; set; }
}

public class CreateInventoryMovementDto
{
    public int ProductId { get; set; }
    public MovementType Type { get; set; }
    public decimal Quantity { get; set; }
    public string? Reason { get; set; }
    public string? DocumentNumber { get; set; }
    public string? UserName { get; set; }
    public string? Source { get; set; }
    public decimal? UnitCost { get; set; }
}

public class InventoryMovementSummaryDto
{
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public int TotalMovements { get; set; }
    public int TotalProducts { get; set; }
    public Dictionary<string, int> MovementsByType { get; set; } = new();
    public Dictionary<string, int> MovementsBySource { get; set; } = new();
    public List<InventoryMovementDto> RecentMovements { get; set; } = new();
}

public class ProductStockHistoryDto
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public List<InventoryMovementDto> Movements { get; set; } = new();
    public Dictionary<DateTime, decimal> StockByDate { get; set; } = new();
}