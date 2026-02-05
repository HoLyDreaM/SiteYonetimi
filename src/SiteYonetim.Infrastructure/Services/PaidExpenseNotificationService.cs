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

    public async Task<IReadOnlyList<OverdueAidatNotificationDto>> GetOverdueAidatAsync(Guid siteId, CancellationToken ct = default)
    {
        var today = DateTime.Today;
        var paidByIncome = await _db.Payments
            .Where(p => p.IncomeId != null && !p.IsDeleted)
            .GroupBy(p => p.IncomeId!.Value)
            .Select(g => new { IncomeId = g.Key, Paid = g.Sum(p => p.Amount) })
            .ToDictionaryAsync(x => x.IncomeId, x => x.Paid, ct);

        var list = await _db.Incomes
            .AsNoTracking()
            .Where(i => i.SiteId == siteId && !i.IsDeleted && i.PaymentEndDate < today)
            .Include(i => i.Apartment)
            .OrderBy(i => i.PaymentEndDate)
            .ThenBy(i => i.Apartment!.BlockOrBuildingName)
            .ThenBy(i => i.Apartment!.ApartmentNumber)
            .Take(50)
            .ToListAsync(ct);

        var result = new List<OverdueAidatNotificationDto>();
        foreach (var i in list)
        {
            var paid = paidByIncome.GetValueOrDefault(i.Id, 0m);
            var remaining = i.Amount - paid;
            if (remaining <= 0) continue;
            if (result.Count >= 20) break;

            result.Add(new OverdueAidatNotificationDto
            {
                IncomeId = i.Id,
                ApartmentInfo = $"{(i.Apartment?.BlockOrBuildingName ?? "")} - {(i.Apartment?.ApartmentNumber ?? "")}".Trim(' ', '-'),
                BlockOrBuilding = i.Apartment?.BlockOrBuildingName ?? "",
                ApartmentNumber = i.Apartment?.ApartmentNumber ?? "",
                OwnerName = i.Apartment?.OwnerName ?? "",
                Amount = i.Amount,
                RemainingAmount = remaining,
                PaymentEndDate = i.PaymentEndDate,
                Year = i.Year,
                Month = i.Month
            });
        }
        return result;
    }
}
