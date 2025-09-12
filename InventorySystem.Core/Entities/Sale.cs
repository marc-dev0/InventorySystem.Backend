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
    public DateTime? ImportedAt { get; set; } // Para trackear cargas
    public string? ImportSource { get; set; } // "TANDIA_EXCEL", "MANUAL", etc.
    
    // Foreign keys
    public int? CustomerId { get; set; }
    public int? EmployeeId { get; set; }
    public int? ImportBatchId { get; set; }
    public int StoreId { get; set; }
    
    // Relationships
    public virtual Customer? Customer { get; set; }
    public virtual Employee? Employee { get; set; }
    public virtual ImportBatch? ImportBatch { get; set; }
    public virtual Store Store { get; set; } = null!;
    public virtual ICollection<SaleDetail> Details { get; set; } = new List<SaleDetail>();
}