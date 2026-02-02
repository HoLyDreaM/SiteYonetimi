using SiteYonetim.Domain.Entities;

namespace SiteYonetim.Domain.Interfaces;

public interface IBankAccountService
{
    Task<BankAccount?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<BankAccount>> GetBySiteIdAsync(Guid siteId, CancellationToken ct = default);
    Task<BankAccount> CreateAsync(BankAccount account, CancellationToken ct = default);
    Task<BankAccount> UpdateAsync(BankAccount account, CancellationToken ct = default);
    Task UpdateBalanceAsync(Guid bankAccountId, decimal amountDelta, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<decimal> GetEffectiveBalanceAsync(Guid bankAccountId, CancellationToken ct = default);
    Task SyncBalancesForSiteAsync(Guid siteId, CancellationToken ct = default);
    Task ReconcileBalanceAsync(Guid bankAccountId, decimal realBalance, CancellationToken ct = default);
}
