namespace InventorySystem.Core.Entities;

public class Purchase : BaseEntity
{
    public string PurchaseNumber { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    public decimal SubTotal { get; set; }
    public decimal Taxes { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
    
    // Relationships
    public virtual ICollection<PurchaseDetail> Details { get; set; } = new List<PurchaseDetail>();
}