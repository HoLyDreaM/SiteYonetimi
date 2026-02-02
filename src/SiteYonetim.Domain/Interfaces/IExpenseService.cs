using SiteYonetim.Domain.Entities;

namespace SiteYonetim.Domain.Interfaces;

public interface IExpenseService
{
    Task<Expense?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Expense>> GetBySiteIdAsync(Guid siteId, DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
    Task<Expense> CreateAsync(Expense expense, CancellationToken ct = default);
    Task<Expense> UpdateAsync(Expense expense, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
