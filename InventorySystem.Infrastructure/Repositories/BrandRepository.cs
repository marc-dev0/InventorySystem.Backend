using Microsoft.EntityFrameworkCore;
using InventorySystem.Core.Entities;
using InventorySystem.Core.Interfaces;
using InventorySystem.Infrastructure.Data;

namespace InventorySystem.Infrastructure.Repositories;

public class BrandRepository : IBrandRepository
{
    private readonly InventoryDbContext _context;

    public BrandRepository(InventoryDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Brand>> GetAllAsync()
    {
        return await _context.Brands
            .Where(b => !b.IsDeleted)
            .ToListAsync();
    }

    public async Task<Brand?> GetByIdAsync(int id)
    {
        return await _context.Brands
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
    }

    public async Task<Brand?> GetByNameAsync(string name)
    {
        return await _context.Brands
            .FirstOrDefaultAsync(b => b.Name == name && !b.IsDeleted);
    }

    public async Task<Brand> AddAsync(Brand brand)
    {
        _context.Brands.Add(brand);
        await _context.SaveChangesAsync();
        return brand;
    }

    public async Task<Brand> UpdateAsync(Brand brand)
    {
        _context.Entry(brand).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return brand;
    }

    public async Task DeleteAsync(int id)
    {
        var brand = await GetByIdAsync(id);
        if (brand != null)
        {
            brand.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }
}