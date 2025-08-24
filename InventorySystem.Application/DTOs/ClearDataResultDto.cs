namespace InventorySystem.Application.DTOs;

public class ClearDataResultDto
{
    public int DeletedProducts { get; set; }
    public int DeletedCategories { get; set; }
    public int DeletedProductStocks { get; set; }
    public int DeletedInventoryMovements { get; set; }
    public int DeletedSales { get; set; }
    public int DeletedSaleDetails { get; set; }
    public string Message { get; set; } = string.Empty;
}