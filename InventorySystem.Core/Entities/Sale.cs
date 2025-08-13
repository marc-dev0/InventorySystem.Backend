using InventorySystem.Core.Entities;

namespace InventorySystem.Core.Entities;

public class Sale : BaseEntity
{
    public string SaleNumber { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    public decimal SubTotal { get; set; }
    public decimal Taxes { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
    
    // Foreign keys
    public int? CustomerId { get; set; }
    
    // Relationships
    public virtual Customer? Customer { get; set; }
    public virtual ICollection<SaleDetail> Details { get; set; } = new List<SaleDetail>();
}