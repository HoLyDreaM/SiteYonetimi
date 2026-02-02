using Microsoft.EntityFrameworkCore;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;
using SiteYonetim.Infrastructure.Data;

namespace SiteYonetim.Infrastructure.Services;

public class QuotationService : IQuotationService
{
    private readonly SiteYonetimDbContext _db;

    public QuotationService(SiteYonetimDbContext db) => _db = db;

    public async Task<Quotation?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.Quotations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public async Task<IReadOnlyList<Quotation>> GetBySiteIdAsync(Guid siteId, CancellationToken ct = default) =>
        await _db.Quotations.AsNoTracking()
            .Where(x => x.SiteId == siteId && !x.IsDeleted)
            .OrderByDescending(x => x.QuotationDate)
            .ToListAsync(ct);

    public async Task<Quotation> CreateAsync(Quotation quotation, CancellationToken ct = default)
    {
        _db.Quotations.Add(quotation);
        await _db.SaveChangesAsync(ct);
        return quotation;
    }

    public async Task<Quotation> UpdateAsync(Quotation quotation, CancellationToken ct = default)
    {
        var existing = await _db.Quotations.FirstOrDefaultAsync(x => x.Id == quotation.Id && !x.IsDeleted, ct);
        if (existing == null) throw new InvalidOperationException("Teklif bulunamadı.");
        existing.CompanyName = quotation.CompanyName;
        existing.QuotationDate = quotation.QuotationDate;
        existing.FilePath = quotation.FilePath;
        existing.MonthlyFee = quotation.MonthlyFee;
        existing.YearlyFee = quotation.YearlyFee;
        existing.Description = quotation.Description;
        await _db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var q = await _db.Quotations.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (q != null)
        {
            q.IsDeleted = true;
            await _db.SaveChangesAsync(ct);
        }
    }
}
