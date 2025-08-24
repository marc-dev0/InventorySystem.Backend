using InventorySystem.Core.Entities;

namespace InventorySystem.Application.DTOs;

public class SaleDto
{
    public int Id { get; set; }
    public string SaleNumber { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal Taxes { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public decimal ItemCount { get; set; }
}

public class SaleDetailsDto : SaleDto
{
    public CustomerDto? Customer { get; set; }
    public List<SaleDetailDto> Details { get; set; } = new();
}

public class SaleDetailDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
}

public class CreateSaleDto
{
    public int? CustomerId { get; set; }
    public string? Notes { get; set; }
    public List<CreateSaleDetailDto> Details { get; set; } = new();
}

public class CreateSaleDetailDto
{
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
