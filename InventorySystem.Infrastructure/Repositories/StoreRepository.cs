using InventorySystem.Core.Entities;
using InventorySystem.Core.Interfaces;
using InventorySystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InventorySystem.Infrastructure.Repositories;

public class StoreRepository : Repository<Store>, IStoreRepository
{
    public StoreRepository(InventoryDbContext context, ILogger<Repository<Store>> logger) 
        : base(context, logger)
    {
    }

    public async Task<Store?> GetByNameAsync(string name)
    {
        return await _context.Set<Store>()
            .FirstOrDefaultAsync(s => s.Name == name);
    }

    public async Task<Store?> GetByCodeAsync(string code)
    {
        return await _context.Set<Store>()
            .FirstOrDefaultAsync(s => s.Code == code);
    }

    public async Task<IEnumerable<Store>> GetActiveStoresAsync()
    {
        return await _context.Set<Store>()
            .Where(s => s.Active)
            .ToListAsync();
    }
}