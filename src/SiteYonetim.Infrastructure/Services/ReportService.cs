using Microsoft.EntityFrameworkCore;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;
using SiteYonetim.Infrastructure.Data;

namespace SiteYonetim.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly SiteYonetimDbContext _db;

    public ReportService(SiteYonetimDbContext db) => _db = db;

    public async Task<MonthlyReportDto> GetMonthlyReportAsync(Guid siteId, int year, int month, CancellationToken ct = default)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);

        var totalIncome = await _db.Incomes
            .Where(x => x.SiteId == siteId && x.Year == year && x.Month == month && !x.IsDeleted && x.Status == IncomeStatus.Paid)
            .SumAsync(x => x.Amount, ct);

        var pendingIncome = await _db.Incomes
            .Where(x => x.SiteId == siteId && x.Year == year && x.Month == month && !x.IsDeleted && x.Status != IncomeStatus.Paid)
            .SumAsync(x => x.Amount, ct);

        // 1) ExcludeFromReport=true olan gider türleri (Aidat vb.)
        var excludedByFlag = await _db.ExpenseTypes
            .Where(et => et.SiteId == siteId && !et.IsDeleted && et.ExcludeFromReport)
            .Select(et => et.Id)
            .ToListAsync(ct);

        // 2) Bu aydaki toplamı Bekleyen aidat ile aynı olan gider türü
        var expensesInPeriod = await _db.Expenses
            .Where(x => x.SiteId == siteId && !x.IsDeleted)
            .Where(x => (x.InvoiceDate ?? x.ExpenseDate) >= start && (x.InvoiceDate ?? x.ExpenseDate) <= end)
            .ToListAsync(ct);

        var typeTotals = expensesInPeriod
            .GroupBy(x => x.ExpenseTypeId)
            .Select(g => new { ExpenseTypeId = g.Key, Total = g.Sum(x => x.Amount) })
            .ToList();

        var excludedByAmount = typeTotals
            .Where(x => x.ExpenseTypeId != Guid.Empty && Math.Abs(x.Total - pendingIncome) < 0.01m)
            .Select(x => x.ExpenseTypeId)
            .ToList();

        var allExcluded = excludedByFlag.Union(excludedByAmount).Distinct().ToList();

        var expenseSum = expensesInPeriod
            .Where(x => !allExcluded.Contains(x.ExpenseTypeId))
            .Sum(x => x.Amount);

        // 3) Bekleyen aidat (gelir) yanlışlıkla gider olarak girilmişse: Toplam giderden çıkar
        // Aidat GELİR'dir, gider değil. Bekleyen tutar giderlerde varsa çıkarılır.
        var totalExpense = pendingIncome > 0 && expenseSum >= pendingIncome
            ? expenseSum - pendingIncome
            : expenseSum;

        return new MonthlyReportDto
        {
            Year = year,
            Month = month,
            TotalIncome = totalIncome,
            PendingIncome = pendingIncome,
            TotalExpense = totalExpense
        };
    }

    public async Task<YearlyReportDto> GetYearlyReportAsync(Guid siteId, int year, CancellationToken ct = default)
    {
        var byMonth = new List<MonthlyReportDto>();
        for (var m = 1; m <= 12; m++)
            byMonth.Add(await GetMonthlyReportAsync(siteId, year, m, ct));

        return new YearlyReportDto
        {
            Year = year,
            TotalIncome = byMonth.Sum(x => x.TotalIncome),
            PendingIncome = byMonth.Sum(x => x.PendingIncome),
            TotalExpense = byMonth.Sum(x => x.TotalExpense),
            ByMonth = byMonth
        };
    }

    public async Task<IReadOnlyList<DebtorDto>> GetDebtorsAsync(Guid siteId, CancellationToken ct = default)
    {
        var apartments = await _db.Apartments
            .Where(x => x.SiteId == siteId && !x.IsDeleted)
            .OrderBy(x => x.BlockOrBuildingName).ThenBy(x => x.ApartmentNumber)
            .ToListAsync(ct);

        var result = new List<DebtorDto>();
        foreach (var apt in apartments)
        {
            var unpaidShares = await _db.ExpenseShares
                .Where(x => x.ApartmentId == apt.Id && !x.IsDeleted && x.Status != ExpenseShareStatus.Paid && x.Status != ExpenseShareStatus.Cancelled)
                .ToListAsync(ct);
            var unpaidShare = unpaidShares.Sum(x => x.Amount + (x.LateFeeAmount ?? 0) - x.PaidAmount);

            var unpaidIncomes = await _db.Incomes
                .Where(x => x.ApartmentId == apt.Id && !x.IsDeleted && x.Status != IncomeStatus.Paid)
                .ToListAsync(ct);
            var unpaidIncome = unpaidIncomes.Sum(x => x.Amount);

            if (unpaidShare <= 0 && unpaidIncome <= 0) continue;

            var debtDates = new List<DateTime>();
            foreach (var s in unpaidShares.Where(x => x.DueDate.HasValue))
                debtDates.Add(s.DueDate!.Value);
            foreach (var i in unpaidIncomes)
                debtDates.Add(i.DueDate);
            var oldestDebt = debtDates.Count > 0 ? debtDates.Min() : (DateTime?)null;
            var today = DateTime.Today;
            var daysOverdue = oldestDebt.HasValue && oldestDebt.Value < today ? (int)(today - oldestDebt.Value).TotalDays : (int?)null;

            result.Add(new DebtorDto
            {
                ApartmentId = apt.Id,
                BlockOrBuildingName = apt.BlockOrBuildingName,
                ApartmentNumber = apt.ApartmentNumber,
                OwnerName = apt.OwnerName,
                OwnerPhone = apt.OwnerPhone,
                UnpaidExpenseShare = unpaidShare,
                UnpaidIncome = unpaidIncome,
                OldestDebtDate = oldestDebt,
                DaysOverdue = daysOverdue
            });
        }
        return result;
    }
}
