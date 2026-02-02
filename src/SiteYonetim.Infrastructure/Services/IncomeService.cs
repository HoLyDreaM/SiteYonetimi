using Microsoft.EntityFrameworkCore;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;
using SiteYonetim.Infrastructure.Data;

namespace SiteYonetim.Infrastructure.Services;

public class IncomeService : IIncomeService
{
    private readonly SiteYonetimDbContext _db;

    public IncomeService(SiteYonetimDbContext db) => _db = db;

    public async Task<Income?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.Incomes.AsNoTracking()
            .Include(x => x.Apartment).Include(x => x.Site)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public async Task<IReadOnlyList<Income>> GetBySiteIdAsync(Guid siteId, int? year = null, int? month = null, CancellationToken ct = default)
    {
        IQueryable<Income> q = _db.Incomes.AsNoTracking()
            .Where(x => x.SiteId == siteId && !x.IsDeleted)
            .Include(x => x.Apartment);
        if (year.HasValue) q = q.Where(x => x.Year == year.Value);
        if (month.HasValue) q = q.Where(x => x.Month == month.Value);
        return await q.OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.Apartment!.BlockOrBuildingName).ThenBy(x => x.Apartment!.ApartmentNumber).ToListAsync(ct);
    }

    public async Task<decimal> GetTotalIncomeBySiteAsync(Guid siteId, int year, int? month = null, CancellationToken ct = default)
    {
        IQueryable<Income> q = _db.Incomes.Where(x => x.SiteId == siteId && x.Year == year && !x.IsDeleted);
        if (month.HasValue) q = q.Where(x => x.Month == month.Value);
        return await q.SumAsync(x => x.Amount, ct);
    }

    public async Task EnsureMonthlyIncomesAsync(int year, int month, CancellationToken ct = default)
    {
        var sites = await _db.Sites.Where(x => !x.IsDeleted).ToListAsync(ct);
        foreach (var site in sites)
        {
            var apartments = await _db.Apartments.Where(x => x.SiteId == site.Id && !x.IsDeleted).ToListAsync(ct);
            var existingKeys = await _db.Incomes
                .Where(x => x.SiteId == site.Id && x.Year == year && x.Month == month && !x.IsDeleted)
                .Select(x => x.ApartmentId)
                .ToListAsync(ct);
            var dueDate = new DateTime(year, month, 1).AddMonths(1).AddDays(-1); // Ay sonu
            var siteStartDay = site.DefaultPaymentStartDay is >= 1 and <= 28 ? site.DefaultPaymentStartDay : 1;
            var siteEndDay = site.DefaultPaymentEndDay is >= 1 and <= 28 ? site.DefaultPaymentEndDay : 20;
            foreach (var apt in apartments)
            {
                if (existingKeys.Contains(apt.Id)) continue;
                var amount = apt.MonthlyDuesAmount ?? (site.DefaultMonthlyDues ?? 0) * apt.ShareRate;
                var startDay = apt.PaymentStartDay is >= 1 and <= 28 ? apt.PaymentStartDay.Value : siteStartDay;
                var endDay = apt.PaymentEndDay is >= 1 and <= 28 ? apt.PaymentEndDay.Value : siteEndDay;
                var paymentStart = new DateTime(year, month, Math.Min(startDay, DateTime.DaysInMonth(year, month)));
                var paymentEnd = new DateTime(year, month, Math.Min(endDay, DateTime.DaysInMonth(year, month)));
                if (amount <= 0) continue;
                var income = new Income
                {
                    SiteId = site.Id,
                    ApartmentId = apt.Id,
                    Year = year,
                    Month = month,
                    Amount = amount,
                    Type = IncomeType.Aidat,
                    Status = IncomeStatus.Unpaid,
                    DueDate = dueDate,
                    PaymentStartDate = paymentStart,
                    PaymentEndDate = paymentEnd,
                    Description = $"{year}-{month:D2} Aidat ({paymentStart:dd.MM}-{paymentEnd:dd.MM})",
                    IsDeleted = false
                };
                _db.Incomes.Add(income);
            }
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task<Income> CreateAsync(Income income, CancellationToken ct = default)
    {
        _db.Incomes.Add(income);
        await _db.SaveChangesAsync(ct);
        return income;
    }

    public async Task MarkAsPaidAsync(Guid incomeId, Guid paymentId, CancellationToken ct = default)
    {
        var income = await _db.Incomes.FirstOrDefaultAsync(x => x.Id == incomeId && !x.IsDeleted, ct);
        if (income != null)
        {
            income.PaymentId = paymentId;
            income.Status = IncomeStatus.Paid;
            await _db.SaveChangesAsync(ct);
        }
    }
}
