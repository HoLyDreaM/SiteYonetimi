using Microsoft.EntityFrameworkCore;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;
using SiteYonetim.Infrastructure.Data;

namespace SiteYonetim.Infrastructure.Services;

/// <summary>
/// Fatura tarihi gelen giderleri varsayılan banka hesabından otomatik olarak düşer.
/// BankTransaction oluşturur ve raporlarda o tarihte gider olarak görünür.
/// </summary>
public class InvoiceExpenseAutoDeductionService : IInvoiceExpenseAutoDeductionService
{
    private readonly SiteYonetimDbContext _db;

    public InvoiceExpenseAutoDeductionService(SiteYonetimDbContext db) => _db = db;

    public async Task ProcessDueExpensesAsync(CancellationToken ct = default)
    {
        var today = DateTime.Today;

        var expensesToProcess = await _db.Expenses
            .Where(e => (e.InvoiceDate ?? e.ExpenseDate) <= today
                && !e.IsDeleted
                && e.Status != ExpenseStatus.Cancelled
                && !_db.BankTransactions.Any(bt => bt.ExpenseId == e.Id && !bt.IsDeleted))
            .Include(e => e.ExpenseType)
            .ToListAsync(ct);

        // Bekleyen aidat: ExcludeFromReport=true veya adında "aidat" geçen giderler gerçek gider değildir, bankadan düşülmez
        expensesToProcess = expensesToProcess
            .Where(e =>
            {
                if (e.ExpenseType == null) return true;
                if (e.ExpenseType.ExcludeFromReport) return false;
                var name = e.ExpenseType.Name ?? "";
                if (name.Contains("aidat", StringComparison.OrdinalIgnoreCase) || name.Contains("AİDAT", StringComparison.OrdinalIgnoreCase))
                    return false;
                return true;
            })
            .ToList();

        foreach (var expense in expensesToProcess)
        {
            var bankAccount = await _db.BankAccounts
                .Where(b => b.SiteId == expense.SiteId && !b.IsDeleted)
                .OrderByDescending(b => b.IsDefault)
                .ThenBy(b => b.BankName)
                .FirstOrDefaultAsync(ct);

            if (bankAccount == null) continue;

            var txDate = expense.InvoiceDate ?? expense.ExpenseDate;
            var transaction = new BankTransaction
            {
                BankAccountId = bankAccount.Id,
                TransactionDate = txDate,
                Amount = -Math.Abs(expense.Amount),
                Type = TransactionType.Expense,
                Description = $"Otomatik gider: {expense.Description}",
                ExpenseId = expense.Id,
                BalanceAfter = bankAccount.CurrentBalance - Math.Abs(expense.Amount),
                IsDeleted = false
            };

            _db.BankTransactions.Add(transaction);
            bankAccount.CurrentBalance -= Math.Abs(expense.Amount);
        }

        if (expensesToProcess.Count > 0)
            await _db.SaveChangesAsync(ct);
    }
}
