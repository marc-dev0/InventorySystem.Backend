namespace InventorySystem.Application.DTOs;

public class InventoryItemDto
{
    public int Id { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public string StoreCode { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal MaximumStock { get; set; }
    public decimal AverageCost { get; set; }
    public decimal TotalValue { get; set; }
    public bool IsLowStock { get; set; }
    public int ProductId { get; set; }
    public int StoreId { get; set; }
}