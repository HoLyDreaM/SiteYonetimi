using Microsoft.EntityFrameworkCore;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;
using SiteYonetim.Infrastructure.Data;

namespace SiteYonetim.Infrastructure.Services;

public class ApartmentService : IApartmentService
{
    private readonly SiteYonetimDbContext _db;

    public ApartmentService(SiteYonetimDbContext db) => _db = db;

    public async Task<Apartment?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.Apartments.AsNoTracking()
            .Include(x => x.Site)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public async Task<IReadOnlyList<Apartment>> GetBySiteIdAsync(Guid siteId, CancellationToken ct = default) =>
        await _db.Apartments.AsNoTracking()
            .Where(x => x.SiteId == siteId && !x.IsDeleted)
            .OrderBy(x => x.BlockOrBuildingName).ThenBy(x => x.ApartmentNumber)
            .ToListAsync(ct);

    public async Task<Apartment> CreateAsync(Apartment apartment, CancellationToken ct = default)
    {
        _db.Apartments.Add(apartment);
        await _db.SaveChangesAsync(ct);
        return apartment;
    }

    public async Task<Apartment> UpdateAsync(Apartment apartment, CancellationToken ct = default)
    {
        var existing = await _db.Apartments.FirstOrDefaultAsync(x => x.Id == apartment.Id && !x.IsDeleted, ct);
        if (existing == null) throw new InvalidOperationException("Daire bulunamadı.");
        existing.ApartmentNumber = apartment.ApartmentNumber;
        existing.BlockOrBuildingName = apartment.BlockOrBuildingName;
        existing.BuildingId = apartment.BuildingId;
        existing.Floor = apartment.Floor;
        existing.ShareRate = apartment.ShareRate;
        existing.MonthlyDuesAmount = apartment.MonthlyDuesAmount;
        existing.OwnerName = apartment.OwnerName;
        existing.OwnerPhone = apartment.OwnerPhone;
        existing.OwnerEmail = apartment.OwnerEmail;
        await _db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var apt = await _db.Apartments.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (apt != null)
        {
            apt.IsDeleted = true;
            await _db.SaveChangesAsync(ct);
        }
    }
}
