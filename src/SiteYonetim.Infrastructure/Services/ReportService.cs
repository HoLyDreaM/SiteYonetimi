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
            .ToListAsync(ct);
        var incomeIds = incomesInPeriod.Select(x => x.Id).ToList();
        var totalIncome = incomeIds.Count > 0
            ? await _db.Payments
                .Where(x => x.IncomeId != null && incomeIds.Contains(x.IncomeId!.Value) && !x.IsDeleted)
                .SumAsync(x => x.Amount, ct)
            : 0m;
        var totalIncomeAmount = incomesInPeriod.Sum(x => x.Amount);
        var pendingIncome = totalIncomeAmount - totalIncome;

        // Ek gelir (Özel Toplama + Diğer) - Aidat dışındaki tüm gelirler
        var extraIncomes = incomesInPeriod.Where(x => x.Type == IncomeType.ExtraCollection || x.Type == IncomeType.Other).ToList();
        var extraIncomeIds = extraIncomes.Select(x => x.Id).ToList();
        var extraCollectionIncome = extraIncomeIds.Count > 0
            ? await _db.Payments
                .Where(x => x.IncomeId != null && extraIncomeIds.Contains(x.IncomeId!.Value) && !x.IsDeleted)
                .SumAsync(x => x.Amount, ct)
            : 0m;
        var extraCollectionAmount = extraIncomes.Sum(x => x.Amount);
        var extraCollectionPending = extraCollectionAmount - extraCollectionIncome;

        // 1) ExcludeFromReport=true olan gider türleri (Aidat vb.)
        var excludedByFlag = await _db.ExpenseTypes
            .Where(et => et.SiteId == siteId && !et.IsDeleted && et.ExcludeFromReport)
            .Select(et => et.Id)
            .ToListAsync(ct);

        // Sadece ExcludeFromReport=true olan gider türlerini hariç tut
        var expensesInPeriod = await _db.Expenses
            .Where(x => x.SiteId == siteId && !x.IsDeleted)
            .Where(x => (x.InvoiceDate ?? x.ExpenseDate) >= start && (x.InvoiceDate ?? x.ExpenseDate) <= end)
            .ToListAsync(ct);

        var totalExpense = expensesInPeriod
            .Where(x => !excludedByFlag.Contains(x.ExpenseTypeId))
            .Sum(x => x.Amount);

        var openingBalance = await GetOpeningBalanceForMonthAsync(siteId, year, month, ct);
        return new MonthlyReportDto
        {
            Year = year,
            Month = month,
            OpeningBalance = openingBalance,
            TotalIncome = totalIncome,
            PendingIncome = pendingIncome,
            ExtraCollectionIncome = extraCollectionIncome,
            ExtraCollectionPending = extraCollectionPending,
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
            .OrderBy(x => x.Type) // Aidat önce, sonra Özel Toplama/Diğer
            .ThenBy(x => x.Apartment.BlockOrBuildingName)
            .ThenBy(x => x.Apartment.ApartmentNumber)
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
            BlockOrBuildingName = i.Apartment?.BlockOrBuildingName ?? "",
            ApartmentNumber = i.Apartment?.ApartmentNumber ?? "",
            OwnerName = i.Apartment?.OwnerName,
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

        var expenseItems = expensesInPeriod
            .Where(x => !excludedByFlag.Contains(x.ExpenseTypeId))
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
            OpeningBalance = summary.OpeningBalance,
            TotalIncome = summary.TotalIncome,
            PendingIncome = summary.PendingIncome,
            ExtraCollectionIncome = summary.ExtraCollectionIncome,
            ExtraCollectionPending = summary.ExtraCollectionPending,
            TotalExpense = summary.TotalExpense,
            IncomeItems = incomeItems,
            ExpenseItems = expenseItems
        };
    }

    public async Task<YearlyReportDto> GetYearlyReportAsync(Guid siteId, int year, CancellationToken ct = default)
    {
        var (cumulativeIncome, cumulativeExpense, openingBalance) = await GetCumulativeTotalsAsync(siteId, year, ct);

        var byMonth = new List<MonthlyReportDto>();
        for (var m = 1; m <= 12; m++)
            byMonth.Add(await GetMonthlyReportAsync(siteId, year, m, ct));

        return new YearlyReportDto
        {
            Year = year,
            TotalIncome = byMonth.Sum(x => x.TotalIncome),
            PendingIncome = byMonth.Sum(x => x.PendingIncome),
            ExtraCollectionIncome = byMonth.Sum(x => x.ExtraCollectionIncome),
            ExtraCollectionPending = byMonth.Sum(x => x.ExtraCollectionPending),
            TotalExpense = byMonth.Sum(x => x.TotalExpense),
            ByMonth = byMonth,
            CumulativeIncomeToDate = cumulativeIncome,
            CumulativeExpenseToDate = cumulativeExpense,
            OpeningBalance = openingBalance
        };
    }

    public async Task<YearlyReportDetailDto> GetYearlyReportDetailAsync(Guid siteId, int year, CancellationToken ct = default)
    {
        var (cumulativeIncome, cumulativeExpense, openingBalance) = await GetCumulativeTotalsAsync(siteId, year, ct);

        var byMonthDetail = new List<MonthlyReportDetailDto>();
        for (var m = 1; m <= 12; m++)
            byMonthDetail.Add(await GetMonthlyReportDetailAsync(siteId, year, m, ct));

        return new YearlyReportDetailDto
        {
            Year = year,
            TotalIncome = byMonthDetail.Sum(x => x.TotalIncome),
            PendingIncome = byMonthDetail.Sum(x => x.PendingIncome),
            ExtraCollectionIncome = byMonthDetail.Sum(x => x.ExtraCollectionIncome),
            ExtraCollectionPending = byMonthDetail.Sum(x => x.ExtraCollectionPending),
            TotalExpense = byMonthDetail.Sum(x => x.TotalExpense),
            ByMonth = byMonthDetail.Cast<MonthlyReportDto>().ToList(),
            ByMonthDetail = byMonthDetail,
            CumulativeIncomeToDate = cumulativeIncome,
            CumulativeExpenseToDate = cumulativeExpense,
            OpeningBalance = openingBalance
        };
    }

    /// <summary>Belirtilen ayın başındaki devir bakiyesi (önceki tüm dönemlerin tahsil - gider). Eylül'de toplanan ek gelir Aralık bakiyesine yansır.</summary>
    private async Task<decimal> GetOpeningBalanceForMonthAsync(Guid siteId, int year, int month, CancellationToken ct)
    {
        var startOfMonth = new DateTime(year, month, 1);
        var incomeIdsBefore = await _db.Incomes
            .Where(x => x.SiteId == siteId && !x.IsDeleted)
            .Where(x => x.Year < year || (x.Year == year && x.Month < month))
            .Select(x => x.Id)
            .ToListAsync(ct);
        var incomeBefore = incomeIdsBefore.Count > 0
            ? await _db.Payments
                .Where(x => x.IncomeId != null && incomeIdsBefore.Contains(x.IncomeId!.Value) && !x.IsDeleted)
                .SumAsync(x => x.Amount, ct)
            : 0m;

        var excludedByFlag = await _db.ExpenseTypes
            .Where(et => et.SiteId == siteId && !et.IsDeleted && et.ExcludeFromReport)
            .Select(et => et.Id)
            .ToListAsync(ct);

        var expensesBefore = await _db.Expenses
            .Where(x => x.SiteId == siteId && !x.IsDeleted)
            .Where(x => (x.InvoiceDate ?? x.ExpenseDate) < startOfMonth)
            .ToListAsync(ct);

        var expenseBefore = expensesBefore
            .Where(x => !excludedByFlag.Contains(x.ExpenseTypeId))
            .Sum(x => x.Amount);

        return incomeBefore - expenseBefore;
    }

    /// <summary>Seçilen yıla kadar (dahil) kümülatif tahsilat ve gider, önceki yıllar devir bakiyesi.</summary>
    private async Task<(decimal CumulativeIncome, decimal CumulativeExpense, decimal OpeningBalance)> GetCumulativeTotalsAsync(Guid siteId, int year, CancellationToken ct)
    {
        // Tahsilat: Income'a bağlı ödemeler (Year <= year)
        var incomeIdsUpToYear = await _db.Incomes
            .Where(x => x.SiteId == siteId && x.Year <= year && !x.IsDeleted)
            .Select(x => x.Id)
            .ToListAsync(ct);
        var cumulativeIncome = await _db.Payments
            .Where(x => x.IncomeId != null && incomeIdsUpToYear.Contains(x.IncomeId!.Value) && !x.IsDeleted)
            .SumAsync(x => x.Amount, ct);

        // Gider: ExcludeFromReport hariç, (InvoiceDate ?? ExpenseDate) <= yıl sonu
        var endOfYear = new DateTime(year, 12, 31);
        var excludedByFlag = await _db.ExpenseTypes
            .Where(et => et.SiteId == siteId && !et.IsDeleted && et.ExcludeFromReport)
            .Select(et => et.Id)
            .ToListAsync(ct);

        var expensesUpToYear = await _db.Expenses
            .Include(x => x.ExpenseType)
            .Where(x => x.SiteId == siteId && !x.IsDeleted)
            .Where(x => (x.InvoiceDate ?? x.ExpenseDate) <= endOfYear)
            .ToListAsync(ct);

        // Sadece ExcludeFromReport=true olan gider türlerini hariç tut. Tüm gerçek giderler sayılır.
        var cumulativeExpense = expensesUpToYear
            .Where(x => !excludedByFlag.Contains(x.ExpenseTypeId))
            .Sum(x => x.Amount);

        // Önceki yıllar devir bakiyesi (seçilen yıldan önceki tahsil - gider)
        decimal incomeBeforeYear = 0;
        decimal expenseBeforeYear = 0;
        if (year > 1)
        {
            var incomeIdsBeforeYear = await _db.Incomes
                .Where(x => x.SiteId == siteId && x.Year < year && !x.IsDeleted)
                .Select(x => x.Id)
                .ToListAsync(ct);
            incomeBeforeYear = await _db.Payments
                .Where(x => x.IncomeId != null && incomeIdsBeforeYear.Contains(x.IncomeId!.Value) && !x.IsDeleted)
                .SumAsync(x => x.Amount, ct);

            var endOfPrevYear = new DateTime(year - 1, 12, 31);
            var expensesBeforeYear = await _db.Expenses
                .Where(x => x.SiteId == siteId && !x.IsDeleted)
                .Where(x => (x.InvoiceDate ?? x.ExpenseDate) <= endOfPrevYear)
                .ToListAsync(ct);

            expenseBeforeYear = expensesBeforeYear
                .Where(x => !excludedByFlag.Contains(x.ExpenseTypeId))
                .Sum(x => x.Amount);
        }

        var openingBalance = year > 1 ? incomeBeforeYear - expenseBeforeYear : 0;

        return (cumulativeIncome, cumulativeExpense, openingBalance);
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
