using InventorySystem.Core.Entities;

namespace InventorySystem.Core.Entities;

public class Store : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public bool Active { get; set; } = true;
    public bool HasInitialStock { get; set; } = false;
    
    // Relationships
    public virtual ICollection<ProductStock> ProductStocks { get; set; } = new List<ProductStock>();
    public virtual ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();
}