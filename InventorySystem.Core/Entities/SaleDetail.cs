using InventorySystem.Core.Entities;

namespace InventorySystem.Core.Entities;

public class SaleDetail : BaseEntity
{
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    
    // Foreign keys
    public int SaleId { get; set; }
    public int ProductId { get; set; }
    
    // Relationships
    public virtual Sale Sale { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}