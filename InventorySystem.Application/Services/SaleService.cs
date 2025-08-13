	using InventorySystem.Application.DTOs;
using InventorySystem.Application.Interfaces;
using InventorySystem.Core.Entities;
using InventorySystem.Core.Interfaces;

namespace InventorySystem.Application.Services;

public class SaleService : ISaleService
{
    private readonly ISaleRepository _saleRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICustomerRepository _customerRepository;

    public SaleService(
        ISaleRepository saleRepository,
        IProductRepository productRepository,
        ICustomerRepository customerRepository)
    {
        _saleRepository = saleRepository;
        _productRepository = productRepository;
        _customerRepository = customerRepository;
    }

    public async Task<IEnumerable<SaleDto>> GetAllAsync()
    {
        var sales = await _saleRepository.GetAllAsync();
        return sales.Select(MapToDto);
    }

    public async Task<SaleDto?> GetByIdAsync(int id)
    {
        var sale = await _saleRepository.GetByIdAsync(id);
        return sale != null ? MapToDto(sale) : null;
    }

    public async Task<SaleDetailsDto?> GetSaleDetailsAsync(int id)
    {
        var sale = await _saleRepository.GetSaleDetailsAsync(id);
        return sale != null ? MapToDetailsDto(sale) : null;
    }

    public async Task<IEnumerable<SaleDto>> GetSalesByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var sales = await _saleRepository.GetSalesByDateRangeAsync(startDate, endDate);
        return sales.Select(MapToDto);
    }

    public async Task<IEnumerable<SaleDto>> GetSalesByCustomerAsync(int customerId)
    {
        var sales = await _saleRepository.GetSalesByCustomerAsync(customerId);
        return sales.Select(MapToDto);
    }

    public async Task<SaleDto> CreateAsync(CreateSaleDto dto)
    {
        // Validate customer if specified
        if (dto.CustomerId.HasValue)
        {
            var customer = await _customerRepository.GetByIdAsync(dto.CustomerId.Value);
            if (customer == null)
            {
                throw new ArgumentException("Customer not found");
            }
        }

        // Validate products and stock
        foreach (var detail in dto.Details)
        {
            var product = await _productRepository.GetByIdAsync(detail.ProductId);
            if (product == null)
            {
                throw new ArgumentException($"Product with ID {detail.ProductId} not found");
            }

            if (product.Stock < detail.Quantity)
            {
                throw new InvalidOperationException($"Insufficient stock for product {product.Name}. Available: {product.Stock}, Requested: {detail.Quantity}");
            }
        }

        // Create sale
        var sale = new Sale
        {
            SaleNumber = await _saleRepository.GenerateSaleNumberAsync(),
            SaleDate = DateTime.UtcNow,
            CustomerId = dto.CustomerId,
            Notes = dto.Notes,
            Details = new List<SaleDetail>()
        };

        // Calculate totals and create details
        decimal subTotal = 0;
        foreach (var detailDto in dto.Details)
        {
            var detail = new SaleDetail
            {
                ProductId = detailDto.ProductId,
                Quantity = detailDto.Quantity,
                UnitPrice = detailDto.UnitPrice,
                Subtotal = detailDto.Quantity * detailDto.UnitPrice
            };

            sale.Details.Add(detail);
            subTotal += detail.Subtotal;

            // Update product stock
            var product = await _productRepository.GetByIdAsync(detailDto.ProductId);
            if (product != null)
            {
                await _productRepository.UpdateStockAsync(product.Id, product.Stock - detailDto.Quantity);
            }
        }

        sale.SubTotal = subTotal;
        sale.Taxes = subTotal * 0.18m; // 18% tax
        sale.Total = sale.SubTotal + sale.Taxes;

        var createdSale = await _saleRepository.AddAsync(sale);
        return MapToDto(createdSale);
    }

    public async Task<object> GetSalesReportAsync(DateTime startDate, DateTime endDate)
    {
        var reportData = await _saleRepository.GetSalesReportAsync(startDate, endDate);
        return new
        {
            Period = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
            Data = reportData
        };
    }

    public async Task<object> GetSalesDashboardAsync()
    {
        var today = DateTime.Today;
        var todaySales = await _saleRepository.GetTotalSalesForDateAsync(today);
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var monthSales = await _saleRepository.GetSalesReportAsync(monthStart, today);

        return new
        {
            TodaySales = todaySales,
            MonthlyData = monthSales,
            LastUpdated = DateTime.UtcNow
        };
    }

    private static SaleDto MapToDto(Sale sale)
    {
        return new SaleDto
        {
            Id = sale.Id,
            SaleNumber = sale.SaleNumber,
            SaleDate = sale.SaleDate,
            SubTotal = sale.SubTotal,
            Taxes = sale.Taxes,
            Total = sale.Total,
            Notes = sale.Notes,
            CustomerId = sale.CustomerId,
            CustomerName = sale.Customer?.Name,
            ItemCount = sale.Details?.Sum(d => d.Quantity) ?? 0
        };
    }

    private static SaleDetailsDto MapToDetailsDto(Sale sale)
    {
        return new SaleDetailsDto
        {
            Id = sale.Id,
            SaleNumber = sale.SaleNumber,
            SaleDate = sale.SaleDate,
            SubTotal = sale.SubTotal,
            Taxes = sale.Taxes,
            Total = sale.Total,
            Notes = sale.Notes,
            CustomerId = sale.CustomerId,
            CustomerName = sale.Customer?.Name,
            ItemCount = sale.Details?.Sum(d => d.Quantity) ?? 0,
            Customer = sale.Customer != null ? MapCustomerToDto(sale.Customer) : null,
            Details = sale.Details?.Select(MapDetailToDto).ToList() ?? new List<SaleDetailDto>()
        };
    }

    private static CustomerDto MapCustomerToDto(Customer customer)
    {
        return new CustomerDto
        {
            Id = customer.Id,
            Name = customer.Name,
            Phone = customer.Phone,
            Email = customer.Email,
            Address = customer.Address,
            Document = customer.Document,
            Active = customer.Active,
            CreatedAt = customer.CreatedAt
        };
    }

    private static SaleDetailDto MapDetailToDto(SaleDetail detail)
    {
        return new SaleDetailDto
        {
            Id = detail.Id,
            ProductId = detail.ProductId,
            ProductName = detail.Product?.Name ?? string.Empty,
            ProductCode = detail.Product?.Code ?? string.Empty,
            Quantity = detail.Quantity,
            UnitPrice = detail.UnitPrice,
            Subtotal = detail.Subtotal
        };
    }
}
