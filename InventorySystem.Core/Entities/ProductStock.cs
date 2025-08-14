using InventorySystem.Core.Entities;

namespace InventorySystem.Core.Entities;

public class ProductStock : BaseEntity
{
    public int CurrentStock { get; set; }
    public int MinimumStock { get; set; }
    public int MaximumStock { get; set; }
    public decimal AverageCost { get; set; }
    
    // Foreign keys
    public int ProductId { get; set; }
    public int StoreId { get; set; }
    public int? ImportBatchId { get; set; }
    
    // Relationships
    public virtual Product Product { get; set; } = null!;
    public virtual Store Store { get; set; } = null!;
    public virtual ImportBatch? ImportBatch { get; set; }
    public virtual ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();
}