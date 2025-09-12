using InventorySystem.Core.Entities;
using InventorySystem.Core.Interfaces;
using InventorySystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InventorySystem.Infrastructure.Repositories;

public class EmployeeRepository : Repository<Employee>, IEmployeeRepository
{
    public EmployeeRepository(InventoryDbContext context, ILogger<Repository<Employee>> logger) : base(context, logger)
    {
    }

    public async Task<Employee?> GetByCodeAsync(string code)
    {
        return await _context.Set<Employee>()
            .FirstOrDefaultAsync(e => e.Code == code && !e.IsDeleted);
    }

    public async Task<IEnumerable<Employee>> GetByStoreIdAsync(int storeId)
    {
        return await _context.Set<Employee>()
            .Where(e => e.StoreId == storeId && !e.IsDeleted)
            .ToListAsync();
    }

    public async Task<Employee?> GetByNameAsync(string name)
    {
        return await _context.Set<Employee>()
            .FirstOrDefaultAsync(e => e.Name == name && !e.IsDeleted);
    }
}