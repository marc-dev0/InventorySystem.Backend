namespace InventorySystem.Core.Entities;

public class ImportBatch : BaseEntity
{
    public string BatchCode { get; set; } = string.Empty; // Código único como "SALES_20250114_001"
    public string BatchType { get; set; } = string.Empty; // "PRODUCTS", "SALES", "STOCK_INITIAL"
    public string FileName { get; set; } = string.Empty;
    public string? StoreCode { get; set; } // Para cargas de stock
    public int TotalRecords { get; set; }
    public int SuccessCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }
    public string? Errors { get; set; } // JSON de errores
    public string? Warnings { get; set; } // JSON de warnings
    public DateTime ImportDate { get; set; }
    public string ImportedBy { get; set; } = string.Empty; // Usuario que hizo la carga
    public int? EmployeeId { get; set; } // ID del empleado si está disponible
    
    // Campos para control de tiempo y estado de procesos
    public DateTime? StartedAt { get; set; } // Cuando inició el proceso
    public DateTime? CompletedAt { get; set; } // Cuando terminó el proceso
    public double? ProcessingTimeSeconds { get; set; } // Tiempo total en segundos
    public bool IsInProgress { get; set; } = false; // Si el proceso está activo
    
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public string? DeleteReason { get; set; }
    public bool IsDeleted { get; set; } = false;
    
    // Relationships
    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    public virtual ICollection<ProductStock> ProductStocks { get; set; } = new List<ProductStock>();
}