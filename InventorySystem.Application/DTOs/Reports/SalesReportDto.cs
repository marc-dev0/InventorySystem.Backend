using System;
using System.Collections.Generic;

namespace InventorySystem.Application.DTOs.Reports
{
    public class SalesReportDto
    {
        public int SaleId { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal TotalCost { get; set; }
        public decimal Profit { get; set; }
        public decimal ProfitMargin { get; set; }
        public int ItemsCount { get; set; }
        public List<SaleDetailReportDto> Details { get; set; } = new();
    }

    public class SaleDetailReportDto
    {
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalCost { get; set; }
        public decimal Profit { get; set; }
        public decimal ProfitMargin { get; set; }
    }

    public class SalesPeriodReportDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal AverageTicket { get; set; }
        public int TotalTransactions { get; set; }
        public int TotalItems { get; set; }
        public List<DailySalesDto> DailySales { get; set; } = new();
        public List<TopProductDto> TopProducts { get; set; } = new();
        public List<CategorySalesDto> SalesByCategory { get; set; } = new();
    }

    public class DailySalesDto
    {
        public DateTime Date { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalProfit { get; set; }
        public int TransactionCount { get; set; }
    }

    public class TopProductDto
    {
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal QuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalProfit { get; set; }
        public int TransactionCount { get; set; }
    }

    public class CategorySalesDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalSales { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal QuantitySold { get; set; }
        public int ProductCount { get; set; }
    }

    public class CustomerAnalysisDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal TotalPurchases { get; set; }
        public int TransactionCount { get; set; }
        public decimal AverageTicket { get; set; }
        public DateTime FirstPurchase { get; set; }
        public DateTime LastPurchase { get; set; }
        public int DaysSinceLastPurchase { get; set; }
    }

}