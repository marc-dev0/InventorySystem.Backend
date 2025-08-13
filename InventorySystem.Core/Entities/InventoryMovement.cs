using InventorySystem.Core.Entities;

namespace InventorySystem.Core.Entities;

public class InventoryMovement : BaseEntity
{
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public MovementType Type { get; set; }
    public int Quantity { get; set; }
    public string? Reason { get; set; }
    public int PreviousStock { get; set; }
    public int NewStock { get; set; }
    
    // Additional tracking fields
    public string? DocumentNumber { get; set; }  // Sale number, purchase number, etc.
    public string? UserName { get; set; }        // Who made the change
    public string? Source { get; set; }          // "Manual", "Tandia_Import", "Sale", etc.
    public decimal? UnitCost { get; set; }       // Cost per unit at time of movement
    public decimal? TotalCost { get; set; }      // Total cost of movement
    
    // Foreign keys
    public int ProductId { get; set; }
    public int StoreId { get; set; }
    public int? ProductStockId { get; set; }     // Reference to specific stock record
    public int? SaleId { get; set; }             // If related to a sale
    public int? PurchaseId { get; set; }         // If related to a purchase
    
    // Relationships
    public virtual Product Product { get; set; } = null!;
    public virtual Store Store { get; set; } = null!;
    public virtual ProductStock? ProductStock { get; set; }
    public virtual Sale? Sale { get; set; }
    public virtual Purchase? Purchase { get; set; }
}