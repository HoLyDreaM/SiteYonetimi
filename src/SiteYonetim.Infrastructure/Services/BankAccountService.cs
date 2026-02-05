using Microsoft.EntityFrameworkCore;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;
using SiteYonetim.Infrastructure.Data;

namespace SiteYonetim.Infrastructure.Services;

public class BankAccountService : IBankAccountService
{
    private readonly SiteYonetimDbContext _db;

    public BankAccountService(SiteYonetimDbContext db) => _db = db;

    public async Task<BankAccount?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.BankAccounts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public async Task<IReadOnlyList<BankAccount>> GetBySiteIdAsync(Guid siteId, CancellationToken ct = default) =>
        await _db.BankAccounts.AsNoTracking()
            .Where(x => x.SiteId == siteId && !x.IsDeleted)
            .OrderBy(x => x.BankName).ThenBy(x => x.AccountNumber)
            .ToListAsync(ct);

    public async Task<BankAccount> CreateAsync(BankAccount account, CancellationToken ct = default)
    {
        account.OpeningBalance = account.CurrentBalance;
        _db.BankAccounts.Add(account);
        await _db.SaveChangesAsync(ct);
        return account;
    }

    public async Task<BankAccount> UpdateAsync(BankAccount account, CancellationToken ct = default)
    {
        var existing = await _db.BankAccounts.FirstOrDefaultAsync(x => x.Id == account.Id && !x.IsDeleted, ct);
        if (existing == null) throw new InvalidOperationException("Banka hesabı bulunamadı.");
        existing.BankName = account.BankName;
        existing.BranchName = account.BranchName;
        existing.AccountNumber = account.AccountNumber;
        existing.IBAN = account.IBAN;
        existing.AccountName = account.AccountName;
        existing.Currency = account.Currency;
        existing.IsDefault = account.IsDefault;
        existing.OpeningBalance = account.OpeningBalance;
        existing.CurrentBalance = account.CurrentBalance;
        await _db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task UpdateBalanceAsync(Guid bankAccountId, decimal amountDelta, CancellationToken ct = default)
    {
        var bank = await _db.BankAccounts.FirstOrDefaultAsync(x => x.Id == bankAccountId && !x.IsDeleted, ct);
        if (bank != null)
        {
            bank.CurrentBalance += amountDelta;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var bank = await _db.BankAccounts.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (bank != null)
        {
            bank.IsDeleted = true;
            bank.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }

    /// <summary>
    /// Gerçek bakiyeyi hesaplar - RAPOR MANTIĞI GİBİ: Başlangıç + Tahsilatlar - Giderler.
    /// Kaynak tablolardan (Payments, Expenses) hesaplanır, BankTransactions'a güvenilmez.
    /// </summary>
    public async Task<decimal> GetEffectiveBalanceAsync(Guid bankAccountId, CancellationToken ct = default)
    {
        var bank = await _db.BankAccounts.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == bankAccountId && !b.IsDeleted, ct);
        if (bank == null) return 0;

        // 1) Tahsilatlar: Bu hesaba yapılan ödemeler (Payments tablosundan - rapor gibi)
        var tahsilat = await _db.Payments
            .Where(p => p.BankAccountId == bankAccountId && !p.IsDeleted)
            .SumAsync(p => p.Amount, ct);

        // 2) Giderler: Sadece varsayılan banka için (giderler oradan ödenir). Rapor mantığı - aidat hariç
        var firstBankId = await _db.BankAccounts
            .Where(b => b.SiteId == bank.SiteId && !b.IsDeleted)
            .OrderByDescending(b => b.IsDefault).ThenBy(b => b.BankName)
            .Select(b => b.Id)
            .FirstOrDefaultAsync(ct);

        decimal gider = 0;
        if (firstBankId == bankAccountId)
        {
            var today = DateTime.Today;
            var aidatTypeIds = await _db.ExpenseTypes
                .Where(et => et.SiteId == bank.SiteId && !et.IsDeleted
                    && (et.ExcludeFromReport || (et.Name != null && (et.Name.Contains("aidat") || et.Name.Contains("Aidat") || et.Name.Contains("AİDAT")))))
                .Select(et => et.Id)
                .ToListAsync(ct);

            gider = await _db.Expenses
                .Where(e => e.SiteId == bank.SiteId && !e.IsDeleted && e.Status != ExpenseStatus.Cancelled
                    && (e.InvoiceDate ?? e.ExpenseDate) <= today
                    && e.ExpenseTypeId != Guid.Empty
                    && (aidatTypeIds.Count == 0 || !aidatTypeIds.Contains(e.ExpenseTypeId)))
                .SumAsync(e => Math.Abs(e.Amount), ct);
        }

        // 3) Bakiye = Başlangıç + Tahsilat - Gider (rapor formülü)
        return bank.OpeningBalance + tahsilat - gider;
    }

    /// <summary>
    /// Düşülmemiş giderleri bankadan düşer ve bakiyeyi günceller.
    /// </summary>
    public async Task SyncBalancesForSiteAsync(Guid siteId, CancellationToken ct = default)
    {
        var today = DateTime.Today;
        var bank = await _db.BankAccounts
            .Where(b => b.SiteId == siteId && !b.IsDeleted)
            .OrderByDescending(b => b.IsDefault).ThenBy(b => b.BankName)
            .FirstOrDefaultAsync(ct);
        if (bank == null) return;

        // Aidat değil: ExcludeFromReport=false VE adında "aidat" yok. InvoiceDate veya ExpenseDate kullan
        var expensesToDeduct = await _db.Expenses
            .Where(e => e.SiteId == siteId && !e.IsDeleted && e.Status != ExpenseStatus.Cancelled
                && (e.InvoiceDate ?? e.ExpenseDate) <= today
                && e.ExpenseTypeId != Guid.Empty
                && !_db.BankTransactions.Any(bt => bt.ExpenseId == e.Id && !bt.IsDeleted)
                && _db.ExpenseTypes.Any(et => et.Id == e.ExpenseTypeId && et.SiteId == siteId && !et.IsDeleted
                    && !et.ExcludeFromReport && (et.Name == null || (!et.Name.Contains("aidat") && !et.Name.Contains("Aidat")))))
            .ToListAsync(ct);

        foreach (var e in expensesToDeduct)
        {
            var amount = Math.Abs(e.Amount);
            var txDate = e.InvoiceDate ?? e.ExpenseDate;
            _db.BankTransactions.Add(new BankTransaction
            {
                BankAccountId = bank.Id,
                TransactionDate = txDate,
                Amount = -amount,
                Type = TransactionType.Expense,
                Description = $"Gider: {e.Description}",
                ExpenseId = e.Id,
                BalanceAfter = bank.CurrentBalance - amount,
                IsDeleted = false
            });
            bank.CurrentBalance -= amount;
        }
        if (expensesToDeduct.Count > 0)
            await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Mutabakat: Banka ekstresindeki gerçek bakiyeyi girince OpeningBalance'ı ayarlar.
    /// Formül: realBalance = OpeningBalance + Tahsilat - Gider → OpeningBalance = realBalance - Tahsilat + Gider
    /// </summary>
    public async Task ReconcileBalanceAsync(Guid bankAccountId, decimal realBalance, CancellationToken ct = default)
    {
        var bank = await _db.BankAccounts.FirstOrDefaultAsync(b => b.Id == bankAccountId && !b.IsDeleted, ct);
        if (bank == null) return;

        var tahsilat = await _db.Payments
            .Where(p => p.BankAccountId == bankAccountId && !p.IsDeleted)
            .SumAsync(p => p.Amount, ct);

        var today = DateTime.Today;
        var firstBankId = await _db.BankAccounts
            .Where(b => b.SiteId == bank.SiteId && !b.IsDeleted)
            .OrderByDescending(b => b.IsDefault).ThenBy(b => b.BankName)
            .Select(b => b.Id)
            .FirstOrDefaultAsync(ct);

        decimal gider = 0;
        if (firstBankId == bankAccountId)
        {
            var aidatTypeIds = await _db.ExpenseTypes
                .Where(et => et.SiteId == bank.SiteId && !et.IsDeleted
                    && (et.ExcludeFromReport || (et.Name != null && (et.Name.Contains("aidat") || et.Name.Contains("Aidat") || et.Name.Contains("AİDAT")))))
                .Select(et => et.Id)
                .ToListAsync(ct);

            gider = await _db.Expenses
                .Where(e => e.SiteId == bank.SiteId && !e.IsDeleted && e.Status != ExpenseStatus.Cancelled
                    && (e.InvoiceDate ?? e.ExpenseDate) <= today
                    && e.ExpenseTypeId != Guid.Empty
                    && (aidatTypeIds.Count == 0 || !aidatTypeIds.Contains(e.ExpenseTypeId)))
                .SumAsync(e => Math.Abs(e.Amount), ct);
        }

        bank.OpeningBalance = realBalance - tahsilat + gider;
        bank.CurrentBalance = realBalance;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<BankAccountTransactionsPagedResult?> GetDetailWithTransactionsPagedAsync(Guid bankAccountId, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var bank = await _db.BankAccounts.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == bankAccountId && !b.IsDeleted, ct);
        if (bank == null) return null;

        var balance = await GetEffectiveBalanceAsync(bankAccountId, ct);

        var query = _db.BankTransactions.AsNoTracking()
            .Where(bt => bt.BankAccountId == bankAccountId && !bt.IsDeleted)
            .OrderByDescending(bt => bt.TransactionDate);

        var totalCount = await query.CountAsync(ct);
        var transactions = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(bt => bt.Payment).ThenInclude(p => p!.Apartment)
            .Include(bt => bt.Expense)
            .ToListAsync(ct);

        var items = transactions.Select(bt =>
        {
            var desc = bt.Description ?? "";
            var aptInfo = bt.Payment?.Apartment != null
                ? $"{bt.Payment.Apartment.BlockOrBuildingName} {bt.Payment.Apartment.ApartmentNumber}".Trim()
                : null;
            return new BankTransactionItemDto
            {
                Date = bt.TransactionDate,
                Description = desc,
                Amount = Math.Abs(bt.Amount),
                IsIncome = bt.Amount > 0,
                ApartmentInfo = aptInfo
            };
        }).ToList();

        return new BankAccountTransactionsPagedResult
        {
            Account = bank,
            Balance = balance,
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

}
