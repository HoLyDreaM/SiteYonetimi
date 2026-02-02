using SiteYonetim.Domain.Entities;

namespace SiteYonetim.Domain.Interfaces;

public interface IExpenseTypeService
{
    Task<ExpenseType?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ExpenseType>> GetBySiteIdAsync(Guid siteId, CancellationToken ct = default);
    Task<ExpenseType> CreateAsync(ExpenseType expenseType, CancellationToken ct = default);
    Task<ExpenseType> UpdateAsync(ExpenseType expenseType, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
