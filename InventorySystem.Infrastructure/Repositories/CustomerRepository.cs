using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using InventorySystem.Core.Entities;
using InventorySystem.Core.Interfaces;
using InventorySystem.Infrastructure.Data;

namespace InventorySystem.Infrastructure.Repositories;

public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(InventoryDbContext context, ILogger<Repository<Customer>> logger) : base(context, logger)
    {
    }

    public async Task<IEnumerable<Customer>> GetActiveCustomersAsync()
    {
        return await _dbSet
            .Where(c => c.Active)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Customer?> GetByDocumentAsync(string document)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => c.Document == document);
    }

    public async Task<IEnumerable<Customer>> SearchCustomersAsync(string searchTerm)
    {
        return await _dbSet
            .Where(c => c.Active && 
                       (c.Name.Contains(searchTerm) || 
                        (c.Document != null && c.Document.Contains(searchTerm)) ||
                        (c.Email != null && c.Email.Contains(searchTerm))))
            .OrderBy(c => c.Name)
            .ToListAsync();
    }
}
