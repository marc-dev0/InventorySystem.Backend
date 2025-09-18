using InventorySystem.Core.Entities;

namespace InventorySystem.Core.Entities;

public class StockTransfer : BaseEntity
{
    public string TransferNumber { get; set; } = string.Empty;
    public DateTime TransferDate { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public TransferStatus Status { get; set; } = TransferStatus.Pending;
    public DateTime? ProcessedAt { get; set; }
    public string? ProcessedByUser { get; set; }
    public DateTime? ImportedAt { get; set; }
    public string? ImportSource { get; set; } = "TANDIA_EXCEL";

    // Foreign keys
    public int OriginStoreId { get; set; }
    public int DestinationStoreId { get; set; }
    public int? ImportBatchId { get; set; }

    // Relationships
    public virtual Store OriginStore { get; set; } = null!;
    public virtual Store DestinationStore { get; set; } = null!;
    public virtual ImportBatch? ImportBatch { get; set; }
    public virtual ICollection<StockTransferDetail> Details { get; set; } = new List<StockTransferDetail>();
}

public enum TransferStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Cancelled = 3
}