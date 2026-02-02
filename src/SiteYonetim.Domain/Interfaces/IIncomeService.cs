using SiteYonetim.Domain.Entities;

namespace SiteYonetim.Domain.Interfaces;

public interface IIncomeService
{
    Task<Income?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Income>> GetBySiteIdAsync(Guid siteId, int? year = null, int? month = null, CancellationToken ct = default);
    Task<decimal> GetTotalIncomeBySiteAsync(Guid siteId, int year, int? month = null, CancellationToken ct = default);
    Task EnsureMonthlyIncomesAsync(int year, int month, CancellationToken ct = default);
    Task<Income> CreateAsync(Income income, CancellationToken ct = default);
    Task MarkAsPaidAsync(Guid incomeId, Guid paymentId, CancellationToken ct = default);
}
