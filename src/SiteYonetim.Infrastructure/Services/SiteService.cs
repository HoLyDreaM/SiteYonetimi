using Microsoft.EntityFrameworkCore;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;
using SiteYonetim.Infrastructure.Data;

namespace SiteYonetim.Infrastructure.Services;

public class SiteService : ISiteService
{
    private readonly SiteYonetimDbContext _db;

    public SiteService(SiteYonetimDbContext db) => _db = db;

    public async Task<Site?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.Sites.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public async Task<IReadOnlyList<Site>> GetUserSitesAsync(Guid userId, CancellationToken ct = default) =>
        await _db.UserSites
            .AsNoTracking()
            .Where(x => x.UserId == userId && !x.IsDeleted)
            .Include(x => x.Site)
            .Where(x => !x.Site!.IsDeleted)
            .Select(x => x.Site!)
            .ToListAsync(ct);

    public async Task<Site> CreateAsync(Site site, Guid? ownerUserId = null, CancellationToken ct = default)
    {
        _db.Sites.Add(site);
        await _db.SaveChangesAsync(ct);
        if (ownerUserId.HasValue)
        {
            _db.UserSites.Add(new UserSite { UserId = ownerUserId.Value, SiteId = site.Id, IsPrimary = true });
            await _db.SaveChangesAsync(ct);
        }
        return site;
    }

    public async Task<Site> UpdateAsync(Site site, CancellationToken ct = default)
    {
        var existing = await _db.Sites.FirstOrDefaultAsync(x => x.Id == site.Id && !x.IsDeleted, ct);
        if (existing == null) throw new InvalidOperationException("Site bulunamadı.");
        existing.Name = site.Name;
        existing.Address = site.Address;
        existing.City = site.City;
        existing.District = site.District;
        existing.TaxOffice = site.TaxOffice;
        existing.TaxNumber = site.TaxNumber;
        existing.LateFeeRate = site.LateFeeRate;
        existing.LateFeeDay = site.LateFeeDay;
        existing.HasMultipleBlocks = site.HasMultipleBlocks;
        existing.DefaultMonthlyDues = site.DefaultMonthlyDues;
        existing.DefaultPaymentStartDay = site.DefaultPaymentStartDay;
        existing.DefaultPaymentEndDay = site.DefaultPaymentEndDay;
        await _db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var site = await _db.Sites.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (site != null)
        {
            site.IsDeleted = true;
            await _db.SaveChangesAsync(ct);
        }
    }
}
