namespace InventorySystem.Application.DTOs;

public class ProductDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal Stock { get; set; }
    public decimal MinimumStock { get; set; }
    public string? Unit { get; set; }
    public bool Active { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateProductDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal Stock { get; set; }
    public decimal MinimumStock { get; set; }
    public string? Unit { get; set; }
    public int CategoryId { get; set; }
    public int? SupplierId { get; set; }
}

public class UpdateProductDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal MinimumStock { get; set; }
    public string? Unit { get; set; }
    public bool Active { get; set; }
    public int CategoryId { get; set; }
    public int? SupplierId { get; set; }
}

public class UpdateStockDto
{
    public decimal NewStock { get; set; }
    public string? Reason { get; set; }
}
