using InventorySystem.Core.Entities;

namespace InventorySystem.Core.Entities;

public class CreditNoteDetail : BaseEntity
{
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; } = 0;
    public decimal SubTotal { get; set; }
    public decimal Taxes { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }

    // Foreign keys
    public int CreditNoteId { get; set; }
    public int ProductId { get; set; }

    // Relationships
    public virtual CreditNote CreditNote { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}