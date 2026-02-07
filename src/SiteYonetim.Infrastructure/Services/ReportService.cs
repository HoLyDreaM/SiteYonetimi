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

        // Tahsil Edilen = Bu ay yapılan GERÇEK para girişleri (Payment tablosu, PaymentDate bu ay)
        var totalIncome = await _db.Payments
            .Where(p => p.SiteId == siteId && p.IncomeId != null && !p.IsDeleted && p.Amount > 0)
            .Where(p => p.PaymentDate >= start && p.PaymentDate <= end)
            .SumAsync(p => p.Amount, ct);

        // Bekleyen Aidat ve Diğer Gelirler (Bu ay için) - Sadece bilgi amaçlı, bakiyeye girmez
        var incomesInPeriod = await _db.Incomes
            .Where(x => x.SiteId == siteId && x.Year == year && x.Month == month && !x.IsDeleted)
            .ToListAsync(ct);
        var incomeIds = incomesInPeriod.Select(x => x.Id).ToList();
        var totalIncomeAmount = incomesInPeriod.Sum(x => x.Amount);
        var incomeCollectedForPeriod = incomeIds.Count > 0
            ? await _db.Payments
                .Where(p => p.IncomeId != null && incomeIds.Contains(p.IncomeId!.Value) && !p.IsDeleted && p.Amount > 0)
                .SumAsync(p => p.Amount, ct)
            : 0m;
        var pendingIncome = Math.Max(0, totalIncomeAmount - incomeCollectedForPeriod);

        // Ek Gelir (Bu ay) = Özel Toplama + Diğer, bu ay tahsil edilen (PaymentDate bu ay)
        var extraIncomeIdsAll = await _db.Incomes
            .Where(x => x.SiteId == siteId && (x.Type == IncomeType.ExtraCollection || x.Type == IncomeType.Other) && !x.IsDeleted)
            .Select(x => x.Id)
            .ToListAsync(ct);
        var extraCollectionIncome = extraIncomeIdsAll.Count > 0
            ? await _db.Payments
                .Where(p => p.IncomeId != null && extraIncomeIdsAll.Contains(p.IncomeId!.Value) && !p.IsDeleted && p.Amount > 0)
                .Where(p => p.PaymentDate >= start && p.PaymentDate <= end)
                .SumAsync(p => p.Amount, ct)
            : 0m;
        var extraIncomes = incomesInPeriod.Where(x => x.Type == IncomeType.ExtraCollection || x.Type == IncomeType.Other).ToList();
        var extraCollectionIdsThisMonth = extraIncomes.Select(x => x.Id).ToList();
        var extraCollectionAmount = extraIncomes.Sum(x => x.Amount);
        var extraCollectionCollected = extraCollectionIdsThisMonth.Count > 0
            ? await _db.Payments
                .Where(p => p.IncomeId != null && extraCollectionIdsThisMonth.Contains(p.IncomeId!.Value) && !p.IsDeleted && p.Amount > 0)
                .SumAsync(p => p.Amount, ct)
            : 0m;
        var extraCollectionPending = Math.Max(0, extraCollectionAmount - extraCollectionCollected);

        // Toplam Gider = Bu ayın TÜM giderleri (gider olarak girilen her kalem giderdir)
        var totalExpense = await _db.Expenses
            .Where(x => x.SiteId == siteId && !x.IsDeleted)
            .Where(x => (x.InvoiceDate ?? x.ExpenseDate) >= start && (x.InvoiceDate ?? x.ExpenseDate) <= end)
            .SumAsync(x => x.Amount, ct);

        // Devir Bakiyesi = Bir önceki ayın kapanış bakiyesi
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
            .Where(p => p.IncomeId != null && incomeIds.Contains(p.IncomeId!.Value) && !p.IsDeleted && p.Amount > 0)
            .GroupBy(p => p.IncomeId!.Value)
            .ToDictionaryAsync(g => g.Key, g => g.Sum(p => p.Amount), ct);

        var incomeTypeNames = new Dictionary<int, string>
        {
            [0] = "Aidat",
            [1] = "Diğer",
            [2] = "Özel Toplama"
        };

        var incomeItems = incomes.Select(i =>
        {
            var apt = i.Apartment;
            var ownerDisplay = apt?.OccupancyType == ApartmentOccupancyType.TenantOccupied
                ? $"Ev Sahibi: {apt?.OwnerName ?? "-"} / Kiracı: {apt?.TenantName ?? "-"}"
                : (apt?.OwnerName ?? "-");
            return new MonthlyReportIncomeItemDto
            {
                BlockOrBuildingName = apt?.BlockOrBuildingName ?? "",
                ApartmentNumber = apt?.ApartmentNumber ?? "",
                OwnerName = ownerDisplay,
                TypeName = incomeTypeNames.GetValueOrDefault((int)i.Type, "Diğer"),
                Description = i.Description,
                Amount = i.Amount,
                PaidAmount = paymentsByIncome.TryGetValue(i.Id, out var paid) ? paid : 0,
                DueDate = i.DueDate
            };
        }).ToList();

        var expensesInPeriod = await _db.Expenses
            .Include(x => x.ExpenseType)
            .Where(x => x.SiteId == siteId && !x.IsDeleted)
            .Where(x => (x.InvoiceDate ?? x.ExpenseDate) >= start && (x.InvoiceDate ?? x.ExpenseDate) <= end)
            .OrderBy(x => x.ExpenseType != null ? x.ExpenseType.Name : "")
            .ThenBy(x => x.ExpenseDate)
            .ToListAsync(ct);

        var expenseItems = expensesInPeriod.Select(e => new MonthlyReportExpenseItemDto
        {
            ExpenseTypeName = e.ExpenseType?.Name ?? "",
            Description = e.Description,
            Amount = e.Amount,
            ExpenseDate = e.InvoiceDate ?? e.ExpenseDate,
            InvoiceNumber = e.InvoiceNumber
        }).ToList();

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
        var (yearIncome, yearExpense, openingBalance) = await GetYearlyTotalsAsync(siteId, year, ct);

        var byMonth = new List<MonthlyReportDto>();
        for (var m = 1; m <= 12; m++)
            byMonth.Add(await GetMonthlyReportAsync(siteId, year, m, ct));

        return new YearlyReportDto
        {
            Year = year,
            TotalIncome = yearIncome,
            PendingIncome = byMonth.Sum(x => x.PendingIncome),
            ExtraCollectionIncome = byMonth.Sum(x => x.ExtraCollectionIncome),
            ExtraCollectionPending = byMonth.Sum(x => x.ExtraCollectionPending),
            TotalExpense = yearExpense,
            ByMonth = byMonth,
            OpeningBalance = openingBalance
        };
    }

    public async Task<YearlyReportDetailDto> GetYearlyReportDetailAsync(Guid siteId, int year, CancellationToken ct = default)
    {
        var (yearIncome, yearExpense, openingBalance) = await GetYearlyTotalsAsync(siteId, year, ct);

        var byMonthDetail = new List<MonthlyReportDetailDto>();
        for (var m = 1; m <= 12; m++)
            byMonthDetail.Add(await GetMonthlyReportDetailAsync(siteId, year, m, ct));

        return new YearlyReportDetailDto
        {
            Year = year,
            TotalIncome = yearIncome,
            PendingIncome = byMonthDetail.Sum(x => x.PendingIncome),
            ExtraCollectionIncome = byMonthDetail.Sum(x => x.ExtraCollectionIncome),
            ExtraCollectionPending = byMonthDetail.Sum(x => x.ExtraCollectionPending),
            TotalExpense = yearExpense,
            ByMonth = byMonthDetail.Cast<MonthlyReportDto>().ToList(),
            ByMonthDetail = byMonthDetail,
            OpeningBalance = openingBalance
        };
    }

    /// <summary>Devir Bakiyesi = Bir önceki ayın kapanış bakiyesi. Sadece Payment (gerçek tahsilat) - Gider.</summary>
    private async Task<decimal> GetOpeningBalanceForMonthAsync(Guid siteId, int year, int month, CancellationToken ct)
    {
        var startOfMonth = new DateTime(year, month, 1);

        var incomeBefore = await _db.Payments
            .Where(p => p.SiteId == siteId && p.IncomeId != null && !p.IsDeleted && p.Amount > 0)
            .Where(p => p.PaymentDate < startOfMonth)
            .SumAsync(p => p.Amount, ct);

        var expenseBefore = await _db.Expenses
            .Where(x => x.SiteId == siteId && !x.IsDeleted)
            .Where(x => (x.InvoiceDate ?? x.ExpenseDate) < startOfMonth)
            .SumAsync(x => x.Amount, ct);

        return incomeBefore - expenseBefore;
    }

    /// <summary>Yıllık rapor için: Önceki Yıllar Devir, Tahsil Edilen (YIL), Yıllık Toplam Gider.
    /// TAHSİLAT = Sadece Payment tablosundaki gerçek para girişleri. Bekleyen/tahakkuk/borç ASLA dahil değil.</summary>
    private async Task<(decimal YearIncome, decimal YearExpense, decimal OpeningBalance)> GetYearlyTotalsAsync(Guid siteId, int year, CancellationToken ct)
    {
        var startOfYear = new DateTime(year, 1, 1);
        var endOfYear = new DateTime(year, 12, 31);

        // Yıllık Tahsil = SADECE ilgili yıl içinde GERÇEKLEŞMİŞ para girişleri (Payment tablosu)
        // Her Payment = banka/kasa girişi, tarihi belli, gerçekten ödenmiş. Çift sayım YOK.
        var yearIncome = await _db.Payments
            .Where(p => p.SiteId == siteId && !p.IsDeleted && p.IncomeId != null && p.Amount > 0)
            .Where(p => p.PaymentDate >= startOfYear && p.PaymentDate <= endOfYear)
            .SumAsync(p => p.Amount, ct);

        // Yıllık Toplam Gider = O yıl içinde girilen TÜM giderler
        var yearExpense = await _db.Expenses
            .Where(x => x.SiteId == siteId && !x.IsDeleted)
            .Where(x => (x.InvoiceDate ?? x.ExpenseDate) >= startOfYear && (x.InvoiceDate ?? x.ExpenseDate) <= endOfYear)
            .SumAsync(x => x.Amount, ct);

        // Önceki Yıllar Devir Bakiyesi (Açılış) = Yıl başındaki kasa (önceki tahsilat - önceki gider)
        var incomeBeforeYear = await _db.Payments
            .Where(p => p.SiteId == siteId && !p.IsDeleted && p.IncomeId != null && p.Amount > 0)
            .Where(p => p.PaymentDate < startOfYear)
            .SumAsync(p => p.Amount, ct);

        var expenseBeforeYear = await _db.Expenses
            .Where(x => x.SiteId == siteId && !x.IsDeleted)
            .Where(x => (x.InvoiceDate ?? x.ExpenseDate) < startOfYear)
            .SumAsync(x => x.Amount, ct);

        var openingBalance = incomeBeforeYear - expenseBeforeYear;

        return (yearIncome, yearExpense, openingBalance);
    }

    /// <summary>Belirli dönem için KASA = TOPLAM TAHSİLAT - TOPLAM GİDER. Sadece Payment (gerçek para girişi).</summary>
    public async Task<PeriodVerificationDto> GetPeriodVerificationAsync(Guid siteId, DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        var totalTahsilat = await _db.Payments
            .Where(p => p.SiteId == siteId && p.IncomeId != null && !p.IsDeleted && p.Amount > 0)
            .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate)
            .SumAsync(p => p.Amount, ct);

        var totalGider = await _db.Expenses
            .Where(x => x.SiteId == siteId && !x.IsDeleted)
            .Where(x => (x.InvoiceDate ?? x.ExpenseDate) >= startDate && (x.InvoiceDate ?? x.ExpenseDate) <= endDate)
            .SumAsync(x => x.Amount, ct);

        return new PeriodVerificationDto
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalTahsilat = totalTahsilat,
            TotalGider = totalGider
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
