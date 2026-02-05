using Microsoft.EntityFrameworkCore;
using SiteYonetim.Domain.Interfaces;
using SiteYonetim.Infrastructure.Data;

namespace SiteYonetim.Infrastructure.Services;

public class PaidExpenseNotificationService : IPaidExpenseNotificationService
{
    private readonly SiteYonetimDbContext _db;

    public PaidExpenseNotificationService(SiteYonetimDbContext db) => _db = db;

    public async Task<IReadOnlyList<PaidExpenseNotificationDto>> GetRecentlyPaidExpensesAsync(Guid siteId, int lastDays = 30, CancellationToken ct = default)
    {
        var fromDate = DateTime.Today.AddDays(-lastDays);
        var list = await _db.BankTransactions
            .AsNoTracking()
            .Where(bt => bt.ExpenseId != null && !bt.IsDeleted && bt.TransactionDate >= fromDate)
            .Join(_db.Expenses.Where(e => e.SiteId == siteId && !e.IsDeleted),
                bt => bt.ExpenseId,
                e => e.Id,
                (bt, e) => new { bt, e })
            .Join(_db.ExpenseTypes.Where(et => !et.IsDeleted),
                x => x.e.ExpenseTypeId,
                et => et.Id,
                (x, et) => new PaidExpenseNotificationDto
                {
                    ExpenseId = x.e.Id,
                    Description = x.e.Description,
                    ExpenseTypeName = et.Name ?? "",
                    Amount = Math.Abs(x.bt.Amount),
                    TransactionDate = x.bt.TransactionDate
                })
            .OrderByDescending(x => x.TransactionDate)
            .Take(20)
            .ToListAsync(ct);
        return list;
    }
}
