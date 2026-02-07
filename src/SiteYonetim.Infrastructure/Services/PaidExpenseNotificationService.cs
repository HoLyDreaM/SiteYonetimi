using Microsoft.EntityFrameworkCore;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;
using SiteYonetim.Infrastructure.Data;

namespace SiteYonetim.Infrastructure.Services;

public class PaidExpenseNotificationService : IPaidExpenseNotificationService
{
    private readonly SiteYonetimDbContext _db;

    public PaidExpenseNotificationService(SiteYonetimDbContext db) => _db = db;

    /// <summary>Süresi geçmiş ve ödenmemiş (Bekliyor) giderler. Status != Paid, BankTransaction yok.</summary>
    public async Task<IReadOnlyList<OverdueExpenseNotificationDto>> GetOverdueExpensesAsync(Guid siteId, CancellationToken ct = default)
    {
        var today = DateTime.Today;
        // Aidat türü giderler (ExcludeFromReport veya adında aidat geçen) hariç
        var list = await _db.Expenses
            .AsNoTracking()
            .Where(e => e.SiteId == siteId && !e.IsDeleted
                && e.Status != ExpenseStatus.Paid
                && e.Status != ExpenseStatus.Cancelled
                && (e.InvoiceDate ?? e.ExpenseDate) < today
                && !_db.BankTransactions.Any(bt => bt.ExpenseId == e.Id && !bt.IsDeleted))
            .Join(_db.ExpenseTypes.Where(et => !et.IsDeleted
                && !et.ExcludeFromReport
                && (et.Name == null || (!et.Name.Contains("aidat") && !et.Name.Contains("Aidat") && !et.Name.Contains("AİDAT")))),
                e => e.ExpenseTypeId,
                et => et.Id,
                (e, et) => new OverdueExpenseNotificationDto
                {
                    ExpenseId = e.Id,
                    SiteId = e.SiteId,
                    Description = e.Description,
                    ExpenseTypeName = et.Name ?? "",
                    Amount = Math.Abs(e.Amount),
                    DueDate = e.InvoiceDate ?? e.ExpenseDate
                })
            .OrderBy(x => x.DueDate)
            .Take(20)
            .ToListAsync(ct);
        return list;
    }

    public async Task<IReadOnlyList<OverdueExpenseNotificationDto>> GetOverdueExpensesForSitesAsync(IEnumerable<Guid> siteIds, CancellationToken ct = default)
    {
        var ids = siteIds.Distinct().ToList();
        if (ids.Count == 0) return Array.Empty<OverdueExpenseNotificationDto>();
        if (ids.Count == 1) return await GetOverdueExpensesAsync(ids[0], ct);

        var today = DateTime.Today;
        var list = await _db.Expenses
            .AsNoTracking()
            .Where(e => ids.Contains(e.SiteId) && !e.IsDeleted
                && e.Status != ExpenseStatus.Paid
                && e.Status != ExpenseStatus.Cancelled
                && (e.InvoiceDate ?? e.ExpenseDate) < today
                && !_db.BankTransactions.Any(bt => bt.ExpenseId == e.Id && !bt.IsDeleted))
            .Join(_db.ExpenseTypes.Where(et => !et.IsDeleted
                && !et.ExcludeFromReport
                && (et.Name == null || (!et.Name.Contains("aidat") && !et.Name.Contains("Aidat") && !et.Name.Contains("AİDAT")))),
                e => e.ExpenseTypeId,
                et => et.Id,
                (e, et) => new OverdueExpenseNotificationDto
                {
                    ExpenseId = e.Id,
                    SiteId = e.SiteId,
                    Description = e.Description,
                    ExpenseTypeName = et.Name ?? "",
                    Amount = Math.Abs(e.Amount),
                    DueDate = e.InvoiceDate ?? e.ExpenseDate
                })
            .OrderBy(x => x.DueDate)
            .Take(30)
            .ToListAsync(ct);
        return list;
    }

    public async Task<IReadOnlyList<OverdueAidatNotificationDto>> GetOverdueAidatAsync(Guid siteId, CancellationToken ct = default)
    {
        var now = DateTime.Now;
        var currentYear = now.Year;
        var currentMonth = now.Month;

        var paidByIncome = await _db.Payments
            .Where(p => p.IncomeId != null && !p.IsDeleted)
            .GroupBy(p => p.IncomeId!.Value)
            .Select(g => new { IncomeId = g.Key, Paid = g.Sum(p => p.Amount) })
            .ToDictionaryAsync(x => x.IncomeId, x => x.Paid, ct);

        var allIncomes = await _db.Incomes
            .AsNoTracking()
            .Where(i => i.SiteId == siteId && !i.IsDeleted)
            .Include(i => i.Apartment)
            .OrderBy(i => i.PaymentEndDate)
            .ThenBy(i => i.Apartment!.BlockOrBuildingName)
            .ThenBy(i => i.Apartment!.ApartmentNumber)
            .ToListAsync(ct);

        var list = allIncomes
            .Where(i => i.Year < currentYear || (i.Year == currentYear && i.Month < currentMonth))
            .Take(50)
            .ToList();

        var result = new List<OverdueAidatNotificationDto>();
        foreach (var i in list)
        {
            var paid = paidByIncome.GetValueOrDefault(i.Id, 0m);
            var remaining = i.Amount - paid;
            if (remaining <= 0) continue;
            if (result.Count >= 20) break;

            var apt = i.Apartment;
            var ownerDisplay = apt?.OccupancyType == ApartmentOccupancyType.TenantOccupied
                ? $"Ev Sahibi: {apt?.OwnerName ?? "-"} / Kiracı: {apt?.TenantName ?? "-"}"
                : (apt?.OwnerName ?? "");
            result.Add(new OverdueAidatNotificationDto
            {
                IncomeId = i.Id,
                SiteId = i.SiteId,
                ApartmentInfo = $"{(apt?.BlockOrBuildingName ?? "")} - {(apt?.ApartmentNumber ?? "")}".Trim(' ', '-'),
                BlockOrBuilding = apt?.BlockOrBuildingName ?? "",
                ApartmentNumber = apt?.ApartmentNumber ?? "",
                OwnerName = ownerDisplay,
                Amount = i.Amount,
                RemainingAmount = remaining,
                PaymentEndDate = i.PaymentEndDate,
                Year = i.Year,
                Month = i.Month
            });
        }
        return result;
    }

    public async Task<IReadOnlyList<OverdueAidatNotificationDto>> GetOverdueAidatForSitesAsync(IEnumerable<Guid> siteIds, CancellationToken ct = default)
    {
        var ids = siteIds.Distinct().ToList();
        if (ids.Count == 0) return Array.Empty<OverdueAidatNotificationDto>();
        if (ids.Count == 1) return await GetOverdueAidatAsync(ids[0], ct);

        var now = DateTime.Now;
        var currentYear = now.Year;
        var currentMonth = now.Month;

        var paidByIncome = await _db.Payments
            .Where(p => p.IncomeId != null && !p.IsDeleted)
            .GroupBy(p => p.IncomeId!.Value)
            .Select(g => new { IncomeId = g.Key, Paid = g.Sum(p => p.Amount) })
            .ToDictionaryAsync(x => x.IncomeId, x => x.Paid, ct);

        var allIncomes = await _db.Incomes
            .AsNoTracking()
            .Where(i => ids.Contains(i.SiteId) && !i.IsDeleted)
            .Include(i => i.Apartment)
            .OrderBy(i => i.DueDate)
            .ThenBy(i => i.SiteId)
            .ThenBy(i => i.Apartment!.BlockOrBuildingName)
            .ThenBy(i => i.Apartment!.ApartmentNumber)
            .ToListAsync(ct);

        var list = allIncomes
            .Where(i => i.Year < currentYear || (i.Year == currentYear && i.Month < currentMonth))
            .Take(100)
            .ToList();

        var result = new List<OverdueAidatNotificationDto>();
        foreach (var i in list)
        {
            var paid = paidByIncome.GetValueOrDefault(i.Id, 0m);
            var remaining = i.Amount - paid;
            if (remaining <= 0) continue;
            if (result.Count >= 30) break;

            var apt = i.Apartment;
            var ownerDisplay = apt?.OccupancyType == ApartmentOccupancyType.TenantOccupied
                ? $"Ev Sahibi: {apt?.OwnerName ?? "-"} / Kiracı: {apt?.TenantName ?? "-"}"
                : (apt?.OwnerName ?? "");
            result.Add(new OverdueAidatNotificationDto
            {
                IncomeId = i.Id,
                SiteId = i.SiteId,
                ApartmentInfo = $"{(apt?.BlockOrBuildingName ?? "")} - {(apt?.ApartmentNumber ?? "")}".Trim(' ', '-'),
                BlockOrBuilding = apt?.BlockOrBuildingName ?? "",
                ApartmentNumber = apt?.ApartmentNumber ?? "",
                OwnerName = ownerDisplay,
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
