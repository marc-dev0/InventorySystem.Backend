using InventorySystem.Application.DTOs;
using InventorySystem.Application.Interfaces;
using InventorySystem.Core.Entities;
using InventorySystem.Core.Interfaces;

namespace InventorySystem.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;

    public CustomerService(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<IEnumerable<CustomerDto>> GetAllAsync()
    {
        var customers = await _customerRepository.GetAllAsync();
        return customers.Select(MapToDto);
    }

    public async Task<CustomerDto?> GetByIdAsync(int id)
    {
        var customer = await _customerRepository.GetByIdAsync(id);
        return customer != null ? MapToDto(customer) : null;
    }

    public async Task<IEnumerable<CustomerDto>> GetActiveCustomersAsync()
    {
        var customers = await _customerRepository.GetActiveCustomersAsync();
        return customers.Select(MapToDto);
    }

    public async Task<CustomerDto?> GetByDocumentAsync(string document)
    {
        var customer = await _customerRepository.GetByDocumentAsync(document);
        return customer != null ? MapToDto(customer) : null;
    }

    public async Task<IEnumerable<CustomerDto>> SearchCustomersAsync(string searchTerm)
    {
        var customers = await _customerRepository.SearchCustomersAsync(searchTerm);
        return customers.Select(MapToDto);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerDto dto)
    {
        var customer = new Customer
        {
            Name = dto.Name,
            Phone = dto.Phone,
            Email = dto.Email,
            Address = dto.Address,
            Document = dto.Document,
            Active = true
        };

        var createdCustomer = await _customerRepository.AddAsync(customer);
        return MapToDto(createdCustomer);
    }

    public async Task UpdateAsync(int id, UpdateCustomerDto dto)
    {
        var customer = await _customerRepository.GetByIdAsync(id);
        if (customer == null)
        {
            throw new KeyNotFoundException("Customer not found");
        }

        customer.Name = dto.Name;
        customer.Phone = dto.Phone;
        customer.Email = dto.Email;
        customer.Address = dto.Address;
        customer.Document = dto.Document;
        customer.Active = dto.Active;

        await _customerRepository.UpdateAsync(customer);
    }

    public async Task DeleteAsync(int id)
    {
        var customer = await _customerRepository.GetByIdAsync(id);
        if (customer == null)
        {
            throw new KeyNotFoundException("Customer not found");
        }

        customer.IsDeleted = true;
        await _customerRepository.UpdateAsync(customer);
    }

    private static CustomerDto MapToDto(Customer customer)
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
}
