using InventorySystem.Core.Entities;

namespace InventorySystem.Core.Entities;

public class StockTransferDetail : BaseEntity
{
    public decimal Quantity { get; set; }
    public decimal? UnitCost { get; set; } // Costo unitario al momento de la transferencia
    public decimal? TotalCost { get; set; } // Costo total de la transferencia
    public string? Notes { get; set; }

    // Foreign keys
    public int StockTransferId { get; set; }
    public int ProductId { get; set; }

    // Relationships
    public virtual StockTransfer StockTransfer { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}