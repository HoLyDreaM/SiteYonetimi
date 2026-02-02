using Microsoft.EntityFrameworkCore;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;
using SiteYonetim.Infrastructure.Data;

namespace SiteYonetim.Infrastructure.Services;

public class ExpenseService : IExpenseService
{
    private readonly SiteYonetimDbContext _db;

    public ExpenseService(SiteYonetimDbContext db)
    {
        _db = db;
    }

    public async Task<Expense?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.Expenses.AsNoTracking()
            .Include(x => x.ExpenseType)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public async Task<IReadOnlyList<Expense>> GetBySiteIdAsync(Guid siteId, DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        IQueryable<Expense> q = _db.Expenses.AsNoTracking()
            .Where(x => x.SiteId == siteId && !x.IsDeleted)
            .Include(x => x.ExpenseType);
        if (from.HasValue) q = q.Where(x => (x.InvoiceDate ?? x.ExpenseDate) >= from.Value);
        if (to.HasValue) q = q.Where(x => (x.InvoiceDate ?? x.ExpenseDate) <= to.Value);
        return await q.OrderByDescending(x => x.InvoiceDate ?? x.ExpenseDate).ToListAsync(ct);
    }

    public async Task<Expense> CreateAsync(Expense expense, CancellationToken ct = default)
    {
        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync(ct);
        await DeductFromBankIfDueAsync(expense, ct);
        return expense;
    }

    public async Task<Expense> UpdateAsync(Expense expense, CancellationToken ct = default)
    {
        var existing = await _db.Expenses.FirstOrDefaultAsync(x => x.Id == expense.Id && !x.IsDeleted, ct);
        if (existing == null) throw new InvalidOperationException("Gider bulunamadı.");
        existing.Description = expense.Description;
        existing.Amount = expense.Amount;
        existing.ExpenseDate = expense.ExpenseDate;
        existing.DueDate = expense.DueDate;
        existing.ExpenseTypeId = expense.ExpenseTypeId;
        existing.InvoiceNumber = expense.InvoiceNumber;
        existing.InvoiceDate = expense.InvoiceDate;
        existing.Notes = expense.Notes;
        await _db.SaveChangesAsync(ct);
        await DeductFromBankIfDueAsync(existing, ct);
        return existing;
    }

    /// <summary>
    /// Fatura tarihi geçmiş ve Aidat olmayan giderleri bankadan hemen düşer.
    /// </summary>
    private async Task DeductFromBankIfDueAsync(Expense expense, CancellationToken ct)
    {
        var expenseDate = expense.InvoiceDate ?? expense.ExpenseDate;
        if (expenseDate.Date > DateTime.Today || expense.Status == ExpenseStatus.Cancelled)
            return;
        if (await _db.BankTransactions.AnyAsync(bt => bt.ExpenseId == expense.Id && !bt.IsDeleted, ct))
            return;

        var expenseType = await _db.ExpenseTypes.FirstOrDefaultAsync(et => et.Id == expense.ExpenseTypeId && !et.IsDeleted, ct);
        if (expenseType != null && (expenseType.ExcludeFromReport || (expenseType.Name ?? "").Contains("aidat", StringComparison.OrdinalIgnoreCase)))
            return;

        var bank = await _db.BankAccounts
            .Where(b => b.SiteId == expense.SiteId && !b.IsDeleted)
            .OrderByDescending(b => b.IsDefault).ThenBy(b => b.BankName)
            .FirstOrDefaultAsync(ct);
        if (bank == null) return;

        _db.BankTransactions.Add(new BankTransaction
        {
            BankAccountId = bank.Id,
            TransactionDate = expenseDate,
            Amount = -Math.Abs(expense.Amount),
            Type = TransactionType.Expense,
            Description = $"Gider: {expense.Description}",
            ExpenseId = expense.Id,
            BalanceAfter = bank.CurrentBalance - Math.Abs(expense.Amount),
            IsDeleted = false
        });
        bank.CurrentBalance -= Math.Abs(expense.Amount);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var e = await _db.Expenses.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (e != null)
        {
            e.IsDeleted = true;
            await _db.SaveChangesAsync(ct);
        }
    }

}
