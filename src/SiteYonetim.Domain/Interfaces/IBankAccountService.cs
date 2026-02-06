using SiteYonetim.Domain.Entities;

namespace SiteYonetim.Domain.Interfaces;

public class BankTransactionItemDto
{
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsIncome { get; set; }
    public string? ApartmentInfo { get; set; }
}

public class BankAccountTransactionsPagedResult
{
    public BankAccount Account { get; set; } = null!;
    public decimal Balance { get; set; }
    public IReadOnlyList<BankTransactionItemDto> Items { get; set; } = Array.Empty<BankTransactionItemDto>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public interface IBankAccountService
{
    Task<BankAccount?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<BankAccount>> GetBySiteIdAsync(Guid siteId, CancellationToken ct = default);
    Task<BankAccount> CreateAsync(BankAccount account, CancellationToken ct = default);
    Task<BankAccount> UpdateAsync(BankAccount account, CancellationToken ct = default);
    Task UpdateBalanceAsync(Guid bankAccountId, decimal amountDelta, CancellationToken ct = default);
    Task<BankAccount?> GetDefaultBankAsync(Guid siteId, CancellationToken ct = default);
    Task<bool> TransferAsync(Guid fromBankAccountId, Guid toBankAccountId, decimal amount, DateTime transactionDate, string? description, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<decimal> GetEffectiveBalanceAsync(Guid bankAccountId, CancellationToken ct = default);
    Task SyncBalancesForSiteAsync(Guid siteId, CancellationToken ct = default);
    Task ReconcileBalanceAsync(Guid bankAccountId, decimal realBalance, CancellationToken ct = default);
    Task<BankAccountTransactionsPagedResult?> GetDetailWithTransactionsPagedAsync(Guid bankAccountId, int page = 1, int pageSize = 20, CancellationToken ct = default);
}
