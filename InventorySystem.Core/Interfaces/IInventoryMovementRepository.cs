using InventorySystem.Core.Entities;

namespace InventorySystem.Core.Interfaces;

public interface IInventoryMovementRepository : IRepository<InventoryMovement>
{
    Task<IEnumerable<InventoryMovement>> GetMovementsByProductAsync(int productId);
    Task<IEnumerable<InventoryMovement>> GetMovementsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<InventoryMovement>> GetMovementsByTypeAsync(MovementType type);
    Task<IEnumerable<InventoryMovement>> GetMovementsBySourceAsync(string source);
    Task<IEnumerable<InventoryMovement>> GetRecentMovementsAsync(int count = 50);
}