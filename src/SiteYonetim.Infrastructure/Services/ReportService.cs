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

        var incomesInPeriod = await _db.Incomes
            .Where(x => x.SiteId == siteId && x.Year == year && x.Month == month && !x.IsDeleted)
            .Select(x => x.Id)
            .ToListAsync(ct);
        var totalIncome = await _db.Payments
            .Where(x => x.IncomeId != null && incomesInPeriod.Contains(x.IncomeId!.Value) && !x.IsDeleted)
            .SumAsync(x => x.Amount, ct);
        var totalIncomeAmount = await _db.Incomes
            .Where(x => x.SiteId == siteId && x.Year == year && x.Month == month && !x.IsDeleted)
            .SumAsync(x => x.Amount, ct);
        var pendingIncome = totalIncomeAmount - totalIncome;

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

    public async Task<MonthlyReportDetailDto> GetMonthlyReportDetailAsync(Guid siteId, int year, int month, CancellationToken ct = default)
    {
        var summary = await GetMonthlyReportAsync(siteId, year, month, ct);

        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);

        var incomes = await _db.Incomes
            .Include(x => x.Apartment)
            .Where(x => x.SiteId == siteId && x.Year == year && x.Month == month && !x.IsDeleted)
            .OrderBy(x => x.Apartment.BlockOrBuildingName).ThenBy(x => x.Apartment.ApartmentNumber)
            .ToListAsync(ct);

        var incomeIds = incomes.Select(x => x.Id).ToList();
        var paymentsByIncome = await _db.Payments
            .Where(x => x.IncomeId != null && incomeIds.Contains(x.IncomeId!.Value) && !x.IsDeleted)
            .GroupBy(x => x.IncomeId!.Value)
            .ToDictionaryAsync(g => g.Key, g => g.Sum(x => x.Amount), ct);

        var incomeTypeNames = new Dictionary<int, string>
        {
            [0] = "Aidat",
            [1] = "Diğer",
            [2] = "Özel Toplama"
        };

        var incomeItems = incomes.Select(i => new MonthlyReportIncomeItemDto
        {
            BlockOrBuildingName = i.Apartment.BlockOrBuildingName,
            ApartmentNumber = i.Apartment.ApartmentNumber,
            OwnerName = i.Apartment.OwnerName,
            TypeName = incomeTypeNames.GetValueOrDefault((int)i.Type, "Diğer"),
            Description = i.Description,
            Amount = i.Amount,
            PaidAmount = paymentsByIncome.TryGetValue(i.Id, out var paid) ? paid : 0,
            DueDate = i.DueDate
        }).ToList();

        var excludedByFlag = await _db.ExpenseTypes
            .Where(et => et.SiteId == siteId && !et.IsDeleted && et.ExcludeFromReport)
            .Select(et => et.Id)
            .ToListAsync(ct);

        var expensesInPeriod = await _db.Expenses
            .Include(x => x.ExpenseType)
            .Where(x => x.SiteId == siteId && !x.IsDeleted)
            .Where(x => (x.InvoiceDate ?? x.ExpenseDate) >= start && (x.InvoiceDate ?? x.ExpenseDate) <= end)
            .ToListAsync(ct);

        var typeTotals = expensesInPeriod
            .GroupBy(x => x.ExpenseTypeId)
            .Select(g => new { ExpenseTypeId = g.Key, Total = g.Sum(x => x.Amount) })
            .ToList();

        var excludedByAmount = typeTotals
            .Where(x => x.ExpenseTypeId != Guid.Empty && Math.Abs(x.Total - summary.PendingIncome) < 0.01m)
            .Select(x => x.ExpenseTypeId)
            .ToList();

        var allExcluded = excludedByFlag.Union(excludedByAmount).Distinct().ToList();

        var expenseItems = expensesInPeriod
            .Where(x => !allExcluded.Contains(x.ExpenseTypeId))
            .OrderBy(x => x.ExpenseType?.Name).ThenBy(x => x.ExpenseDate)
            .Select(e => new MonthlyReportExpenseItemDto
            {
                ExpenseTypeName = e.ExpenseType?.Name ?? "",
                Description = e.Description,
                Amount = e.Amount,
                ExpenseDate = e.InvoiceDate ?? e.ExpenseDate,
                InvoiceNumber = e.InvoiceNumber
            })
            .ToList();

        return new MonthlyReportDetailDto
        {
            Year = year,
            Month = month,
            TotalIncome = summary.TotalIncome,
            PendingIncome = summary.PendingIncome,
            TotalExpense = summary.TotalExpense,
            IncomeItems = incomeItems,
            ExpenseItems = expenseItems
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

    public async Task<YearlyReportDetailDto> GetYearlyReportDetailAsync(Guid siteId, int year, CancellationToken ct = default)
    {
        var byMonthDetail = new List<MonthlyReportDetailDto>();
        for (var m = 1; m <= 12; m++)
            byMonthDetail.Add(await GetMonthlyReportDetailAsync(siteId, year, m, ct));

        return new YearlyReportDetailDto
        {
            Year = year,
            TotalIncome = byMonthDetail.Sum(x => x.TotalIncome),
            PendingIncome = byMonthDetail.Sum(x => x.PendingIncome),
            TotalExpense = byMonthDetail.Sum(x => x.TotalExpense),
            ByMonth = byMonthDetail.Cast<MonthlyReportDto>().ToList(),
            ByMonthDetail = byMonthDetail
        };
    }

    /// <summary>Sadece aidat ve ek para toplama borçlarını getirir. Gider (elektrik vb.) borçları dahil değildir.</summary>
    public async Task<IReadOnlyList<DebtorDto>> GetDebtorsAsync(Guid siteId, CancellationToken ct = default)
    {
        var apartments = await _db.Apartments
            .Where(x => x.SiteId == siteId && !x.IsDeleted)
            .OrderBy(x => x.BlockOrBuildingName).ThenBy(x => x.ApartmentNumber)
            .ToListAsync(ct);

        var result = new List<DebtorDto>();
        foreach (var apt in apartments)
        {
            var unpaidIncomes = await _db.Incomes
                .Where(x => x.ApartmentId == apt.Id && !x.IsDeleted)
                .ToListAsync(ct);
            var unpaidIncome = 0m;
            foreach (var inc in unpaidIncomes)
            {
                var paid = await _db.Payments.Where(p => p.IncomeId == inc.Id && !p.IsDeleted).SumAsync(x => x.Amount, ct);
                unpaidIncome += Math.Max(0, inc.Amount - paid);
            }

            if (unpaidIncome <= 0) continue;

            var debtDates = unpaidIncomes.Select(i => i.DueDate).ToList();
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
                UnpaidExpenseShare = 0,
                UnpaidIncome = unpaidIncome,
                OldestDebtDate = oldestDebt,
                DaysOverdue = daysOverdue
            });
        }
        return result;
    }

    public async Task<HazirunCetveliDto> GetHazirunCetveliAsync(Guid siteId, DateTime? date = null, CancellationToken ct = default)
    {
        var site = await _db.Sites.AsNoTracking().FirstOrDefaultAsync(s => s.Id == siteId && !s.IsDeleted, ct);
        var apartments = await _db.Apartments
            .AsNoTracking()
            .Where(x => x.SiteId == siteId && !x.IsDeleted)
            .OrderBy(x => x.BlockOrBuildingName).ThenBy(x => x.ApartmentNumber)
            .ToListAsync(ct);

        var items = apartments.Select(a => new HazirunCetveliItemDto
        {
            BlockOrBuildingName = a.BlockOrBuildingName ?? "",
            ApartmentNumber = a.ApartmentNumber ?? "",
            KatMaliki = a.OwnerName ?? "-",
            VekilAdi = null
        }).ToList();

        return new HazirunCetveliDto
        {
            SiteName = site?.Name ?? "",
            Date = date ?? DateTime.Today,
            Items = items
        };
    }
}
