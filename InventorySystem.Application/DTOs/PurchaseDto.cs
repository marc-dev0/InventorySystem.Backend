namespace InventorySystem.Application.DTOs;

public class PurchaseDto
{
    public int Id { get; set; }
    public string PurchaseNumber { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal Taxes { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
    public int ItemCount { get; set; }
}

public class PurchaseDetailsDto : PurchaseDto
{
    public List<PurchaseDetailDto> Details { get; set; } = new();
}

public class PurchaseDetailDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
}

public class CreatePurchaseDto
{
    public string? Notes { get; set; }
    public List<CreatePurchaseDetailDto> Details { get; set; } = new();
}

public class CreatePurchaseDetailDto
{
    public int ProductId { get; set; }
    public int SupplierId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}