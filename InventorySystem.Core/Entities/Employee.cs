using InventorySystem.Core.Entities;

namespace InventorySystem.Core.Entities;

public class Employee : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Document { get; set; }
    public string? Position { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool Active { get; set; } = true;
    public DateTime HireDate { get; set; } = DateTime.UtcNow;
    
    // Foreign keys
    public int? StoreId { get; set; }
    
    // Relationships
    public virtual Store? Store { get; set; }
    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
}