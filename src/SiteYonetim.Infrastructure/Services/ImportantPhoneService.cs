using Microsoft.EntityFrameworkCore;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;
using SiteYonetim.Infrastructure.Data;

namespace SiteYonetim.Infrastructure.Services;

public class ImportantPhoneService : IImportantPhoneService
{
    private readonly SiteYonetimDbContext _db;

    public ImportantPhoneService(SiteYonetimDbContext db) => _db = db;

    public async Task<ImportantPhone?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.ImportantPhones.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public async Task<IReadOnlyList<ImportantPhone>> GetBySiteIdAsync(Guid siteId, CancellationToken ct = default) =>
        await _db.ImportantPhones.AsNoTracking()
            .Where(x => x.SiteId == siteId && !x.IsDeleted)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

    public async Task<ImportantPhone> CreateAsync(ImportantPhone phone, CancellationToken ct = default)
    {
        _db.ImportantPhones.Add(phone);
        await _db.SaveChangesAsync(ct);
        return phone;
    }

    public async Task<ImportantPhone> UpdateAsync(ImportantPhone phone, CancellationToken ct = default)
    {
        var existing = await _db.ImportantPhones.FirstOrDefaultAsync(x => x.Id == phone.Id && !x.IsDeleted, ct);
        if (existing == null) throw new InvalidOperationException("Kayıt bulunamadı.");
        existing.Name = phone.Name;
        existing.Phone = phone.Phone;
        existing.ExtraInfo = phone.ExtraInfo;
        await _db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var p = await _db.ImportantPhones.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (p != null)
        {
            p.IsDeleted = true;
            await _db.SaveChangesAsync(ct);
        }
    }
}
