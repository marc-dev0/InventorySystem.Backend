namespace InventorySystem.Core.Entities;

public class Supplier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public bool Active { get; set; } = true;
    
    // Relationships
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    public virtual ICollection<PurchaseDetail> PurchaseDetails { get; set; } = new List<PurchaseDetail>();
}