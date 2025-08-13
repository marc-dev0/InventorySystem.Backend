namespace InventorySystem.Core.Entities;

public class PurchaseDetail : BaseEntity
{
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    
    // Foreign keys
    public int PurchaseId { get; set; }
    public int ProductId { get; set; }
    public int? SupplierId { get; set; }
    
    // Relationships
    public virtual Purchase Purchase { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
    public virtual Supplier? Supplier { get; set; }
}