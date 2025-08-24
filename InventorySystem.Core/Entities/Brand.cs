namespace InventorySystem.Core.Entities;

public class Brand : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Active { get; set; } = true;
    
    // Relationships
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}