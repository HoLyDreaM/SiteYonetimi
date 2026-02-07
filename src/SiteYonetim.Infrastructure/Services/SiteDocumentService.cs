using Microsoft.EntityFrameworkCore;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;
using SiteYonetim.Infrastructure.Data;

namespace SiteYonetim.Infrastructure.Services;

public class SiteDocumentService : ISiteDocumentService
{
    private readonly SiteYonetimDbContext _db;

    public SiteDocumentService(SiteYonetimDbContext db) => _db = db;

    public async Task<SiteDocument?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.SiteDocuments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public async Task<IReadOnlyList<SiteDocument>> GetBySiteIdAsync(Guid siteId, CancellationToken ct = default) =>
        await _db.SiteDocuments.AsNoTracking()
            .Where(x => x.SiteId == siteId && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

    public async Task<SiteDocument> CreateAsync(SiteDocument document, CancellationToken ct = default)
    {
        _db.SiteDocuments.Add(document);
        await _db.SaveChangesAsync(ct);
        return document;
    }

    public async Task<SiteDocument> UpdateAsync(SiteDocument document, CancellationToken ct = default)
    {
        var existing = await _db.SiteDocuments.FirstOrDefaultAsync(x => x.Id == document.Id && !x.IsDeleted, ct);
        if (existing == null) throw new InvalidOperationException("Evrak bulunamadı.");
        existing.Name = document.Name;
        existing.Description = document.Description;
        existing.FilePath = document.FilePath;
        existing.FileName = document.FileName;
        await _db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var doc = await _db.SiteDocuments.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (doc != null)
        {
            doc.IsDeleted = true;
            await _db.SaveChangesAsync(ct);
        }
    }
}
