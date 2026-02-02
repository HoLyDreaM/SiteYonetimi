using SiteYonetim.Domain.Entities;

namespace SiteYonetim.Domain.Interfaces;

public interface IExpenseShareService
{
    Task<ExpenseShare?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ExpenseShare>> GetByApartmentIdAsync(Guid apartmentId, bool onlyUnpaid = false, CancellationToken ct = default);
    Task<IReadOnlyList<ExpenseShare>> GetBySiteIdAsync(Guid siteId, Guid? apartmentId = null, int? status = null, CancellationToken ct = default);
    Task ApplyLateFeesAsync(Guid siteId, CancellationToken ct = default);
}
