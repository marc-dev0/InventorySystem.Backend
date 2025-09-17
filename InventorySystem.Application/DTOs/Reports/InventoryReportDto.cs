using System;
using System.Collections.Generic;

namespace InventorySystem.Application.DTOs.Reports
{
    public class InventoryReportDto
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public string StoreCode { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
        public decimal CurrentStock { get; set; }
        public decimal MinimumStock { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal TotalValue { get; set; }
        public bool IsLowStock { get; set; }
        public bool IsOutOfStock { get; set; }
        public DateTime LastMovementDate { get; set; }
        public int DaysWithoutMovement { get; set; }
    }

    public class StockCriticalReportDto
    {
        public List<InventoryReportDto> LowStockItems { get; set; } = new();
        public List<InventoryReportDto> OutOfStockItems { get; set; } = new();
        public decimal TotalLowStockValue { get; set; }
        public int TotalLowStockItems { get; set; }
        public int TotalOutOfStockItems { get; set; }
    }

    public class InventoryValuationReportDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
        public decimal TotalUnits { get; set; }
        public decimal TotalPurchaseValue { get; set; }
        public decimal TotalSaleValue { get; set; }
        public decimal PotentialProfit { get; set; }
        public int ProductCount { get; set; }
    }

    public class ProductMovementReportDto
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string MovementType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal PreviousStock { get; set; }
        public decimal NewStock { get; set; }
        public DateTime MovementDate { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}