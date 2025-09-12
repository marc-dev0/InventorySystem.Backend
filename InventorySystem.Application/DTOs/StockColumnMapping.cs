namespace InventorySystem.Application.DTOs;

public class StockColumnMapping
{
    public int CodeColumn { get; set; } = 2;      // Columna B
    public int StockColumn { get; set; } = 11;    // Columna K
    public int MinStockColumn { get; set; } = 12; // Columna L
    public int StartRow { get; set; } = 2;        // Fila 2 (después del header)
}