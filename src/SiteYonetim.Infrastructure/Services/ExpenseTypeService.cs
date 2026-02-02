using Microsoft.EntityFrameworkCore;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;
using SiteYonetim.Infrastructure.Data;

namespace SiteYonetim.Infrastructure.Services;

public class ExpenseTypeService : IExpenseTypeService
{
    private readonly SiteYonetimDbContext _db;

    public ExpenseTypeService(SiteYonetimDbContext db) => _db = db;

    public async Task<ExpenseType?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.ExpenseTypes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public async Task<IReadOnlyList<ExpenseType>> GetBySiteIdAsync(Guid siteId, CancellationToken ct = default) =>
        await _db.ExpenseTypes.AsNoTracking()
            .Where(x => x.SiteId == siteId && !x.IsDeleted)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

    public async Task<ExpenseType> CreateAsync(ExpenseType expenseType, CancellationToken ct = default)
    {
        _db.ExpenseTypes.Add(expenseType);
        await _db.SaveChangesAsync(ct);
        return expenseType;
    }

    public async Task<ExpenseType> UpdateAsync(ExpenseType expenseType, CancellationToken ct = default)
    {
        var existing = await _db.ExpenseTypes.FirstOrDefaultAsync(x => x.Id == expenseType.Id && !x.IsDeleted, ct);
        if (existing == null) throw new InvalidOperationException("Gider türü bulunamadı.");
        existing.Name = expenseType.Name;
        existing.Description = expenseType.Description;
        existing.ExcludeFromReport = expenseType.ExcludeFromReport;
        await _db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var et = await _db.ExpenseTypes.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (et != null)
        {
            et.IsDeleted = true;
            await _db.SaveChangesAsync(ct);
        }
    }
}
