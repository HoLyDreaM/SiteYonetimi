using Microsoft.EntityFrameworkCore;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;
using SiteYonetim.Infrastructure.Data;

namespace SiteYonetim.Infrastructure.Services;

public class ExpenseShareService : IExpenseShareService
{
    private readonly SiteYonetimDbContext _db;

    public ExpenseShareService(SiteYonetimDbContext db) => _db = db;

    public async Task<ExpenseShare?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.ExpenseShares.AsNoTracking()
            .Include(x => x.Expense).ThenInclude(e => e!.ExpenseType)
            .Include(x => x.Apartment)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public async Task<IReadOnlyList<ExpenseShare>> GetByApartmentIdAsync(Guid apartmentId, bool onlyUnpaid = false, CancellationToken ct = default)
    {
        IQueryable<ExpenseShare> q = _db.ExpenseShares.AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId && !x.IsDeleted)
            .Include(x => x.Expense).ThenInclude(e => e!.ExpenseType);
        if (onlyUnpaid) q = q.Where(x => x.Status == ExpenseShareStatus.Pending || x.Status == ExpenseShareStatus.Overdue || x.Status == ExpenseShareStatus.PartiallyPaid);
        return await q.OrderByDescending(x => x.DueDate).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ExpenseShare>> GetBySiteIdAsync(Guid siteId, Guid? apartmentId = null, int? status = null, CancellationToken ct = default)
    {
        IQueryable<ExpenseShare> q = _db.ExpenseShares.AsNoTracking()
            .Where(x => x.Expense!.SiteId == siteId && !x.IsDeleted)
            .Include(x => x.Expense).ThenInclude(e => e!.ExpenseType)
            .Include(x => x.Apartment);
        if (apartmentId.HasValue) q = q.Where(x => x.ApartmentId == apartmentId.Value);
        if (status.HasValue) q = q.Where(x => (int)x.Status == status.Value);
        return await q.OrderByDescending(x => x.DueDate).ToListAsync(ct);
    }

    public async Task ApplyLateFeesAsync(Guid siteId, CancellationToken ct = default)
    {
        var site = await _db.Sites.FirstOrDefaultAsync(x => x.Id == siteId && !x.IsDeleted, ct);
        if (site?.LateFeeRate == null || site.LateFeeDay == null) return;

        var overdue = await _db.ExpenseShares
            .Where(x => x.Expense!.SiteId == siteId && !x.IsDeleted)
            .Where(x => x.Status == ExpenseShareStatus.Pending || x.Status == ExpenseShareStatus.Overdue)
            .Where(x => x.DueDate != null && x.DueDate.Value < DateTime.UtcNow.Date)
            .Where(x => x.LateFeeAmount == null || x.LateFeeAmount == 0)
            .Include(x => x.Expense)
            .ToListAsync(ct);

        foreach (var share in overdue)
        {
            var daysLate = (DateTime.UtcNow.Date - share.DueDate!.Value).Days;
            if (daysLate < site.LateFeeDay) continue;
            var lateFee = Math.Round(share.Amount * (site.LateFeeRate!.Value / 100m) * (daysLate / 30), 2);
            share.LateFeeAmount = (share.LateFeeAmount ?? 0) + lateFee;
            share.Status = ExpenseShareStatus.Overdue;
        }
        await _db.SaveChangesAsync(ct);
    }
}
