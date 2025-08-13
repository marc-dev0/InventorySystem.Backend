namespace InventorySystem.Core.Entities;

public class Customer : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Document { get; set; }
    public bool Active { get; set; } = true;
    
    // Relationships
    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
}