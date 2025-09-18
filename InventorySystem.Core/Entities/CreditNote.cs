using InventorySystem.Core.Entities;

namespace InventorySystem.Core.Entities;

public class CreditNote : BaseEntity
{
    public string CreditNoteNumber { get; set; } = string.Empty;
    public DateTime CreditNoteDate { get; set; } = DateTime.UtcNow;
    public decimal SubTotal { get; set; }
    public decimal Taxes { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
    public string Reason { get; set; } = string.Empty; // DEVOLUCION_MERCADERIA, ANULACION_VENTA, etc.
    public DateTime? ImportedAt { get; set; }
    public string? ImportSource { get; set; } = "TANDIA_EXCEL";

    // Foreign keys
    public int? CustomerId { get; set; }
    public int? OriginalSaleId { get; set; } // Venta original que se está devolviendo
    public int? ImportBatchId { get; set; }
    public int StoreId { get; set; }

    // Relationships
    public virtual Customer? Customer { get; set; }
    public virtual Sale? OriginalSale { get; set; }
    public virtual ImportBatch? ImportBatch { get; set; }
    public virtual Store Store { get; set; } = null!;
    public virtual ICollection<CreditNoteDetail> Details { get; set; } = new List<CreditNoteDetail>();
}