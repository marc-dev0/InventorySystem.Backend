namespace InventorySystem.Core.Entities;

public class Purchase : BaseEntity
{
    public string PurchaseNumber { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    public decimal SubTotal { get; set; }
    public decimal Taxes { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
    public DateTime? ImportedAt { get; set; }
    public string? ImportSource { get; set; } = "TANDIA_EXCEL";
    public string? SupplierName { get; set; } // Nombre del proveedor desde Excel

    // Foreign keys
    public int? SupplierId { get; set; } // Referencia a proveedor si existe
    public int? ImportBatchId { get; set; }
    public int StoreId { get; set; } // Tienda a la que va la compra

    // Relationships
    public virtual Supplier? Supplier { get; set; }
    public virtual ImportBatch? ImportBatch { get; set; }
    public virtual Store Store { get; set; } = null!;
    public virtual ICollection<PurchaseDetail> Details { get; set; } = new List<PurchaseDetail>();
}