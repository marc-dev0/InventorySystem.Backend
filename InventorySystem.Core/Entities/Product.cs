using InventorySystem.Core.Entities;

namespace InventorySystem.Core.Entities;

public class Product : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal Stock { get; set; } // Deprecated - use ProductStocks instead
    public decimal MinimumStock { get; set; } // Deprecated - use ProductStocks instead
    public string? Unit { get; set; } // Ex: "unit", "kg", "liter"
    public bool Active { get; set; } = true;
    
    // Foreign keys
    public int CategoryId { get; set; }
    public int? SupplierId { get; set; }
    public int? BrandId { get; set; }
    public int? ImportBatchId { get; set; }
    
    // Relationships
    public virtual Category Category { get; set; } = null!;
    public virtual Supplier? Supplier { get; set; }
    public virtual Brand? Brand { get; set; }
    public virtual ImportBatch? ImportBatch { get; set; }
    public virtual ICollection<SaleDetail> SaleDetails { get; set; } = new List<SaleDetail>();
    public virtual ICollection<PurchaseDetail> PurchaseDetails { get; set; } = new List<PurchaseDetail>();
    public virtual ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();
    public virtual ICollection<ProductStock> ProductStocks { get; set; } = new List<ProductStock>();
}