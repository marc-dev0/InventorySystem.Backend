using InventorySystem.Core.Entities;

namespace InventorySystem.Core.Interfaces;

public interface ICreditNoteRepository : IRepository<CreditNote>
{
    Task<IEnumerable<CreditNote>> GetCreditNotesByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<CreditNote?> GetCreditNoteDetailsAsync(int creditNoteId);
    Task<string> GenerateCreditNoteNumberAsync();
    Task<decimal> GetTotalCreditNotesForDateAsync(DateTime date);
    Task<IEnumerable<CreditNote>> GetCreditNotesByStoreAsync(int storeId);
    Task<IEnumerable<CreditNote>> GetCreditNotesByCustomerAsync(int customerId);
}