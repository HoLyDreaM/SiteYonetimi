using Microsoft.EntityFrameworkCore;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;
using SiteYonetim.Infrastructure.Data;

namespace SiteYonetim.Infrastructure.Services;

public class ResidentContactService : IResidentContactService
{
    private readonly SiteYonetimDbContext _db;

    public ResidentContactService(SiteYonetimDbContext db) => _db = db;

    public async Task<ResidentContact?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.ResidentContacts.AsNoTracking()
            .Include(x => x.Apartment)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public async Task<IReadOnlyList<ResidentContact>> GetBySiteIdAsync(Guid siteId, CancellationToken ct = default) =>
        await _db.ResidentContacts.AsNoTracking()
            .Include(x => x.Apartment)
            .Where(x => x.SiteId == siteId && !x.IsDeleted)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

    public async Task<ResidentContact> CreateAsync(ResidentContact contact, CancellationToken ct = default)
    {
        _db.ResidentContacts.Add(contact);
        await _db.SaveChangesAsync(ct);
        return contact;
    }

    public async Task<ResidentContact> UpdateAsync(ResidentContact contact, CancellationToken ct = default)
    {
        var existing = await _db.ResidentContacts.FirstOrDefaultAsync(x => x.Id == contact.Id && !x.IsDeleted, ct);
        if (existing == null) throw new InvalidOperationException("Kayıt bulunamadı.");
        existing.ApartmentId = contact.ApartmentId;
        existing.Name = contact.Name;
        existing.Phone = contact.Phone;
        existing.ContactType = contact.ContactType;
        existing.Notes = contact.Notes;
        await _db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var c = await _db.ResidentContacts.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (c != null)
        {
            c.IsDeleted = true;
            await _db.SaveChangesAsync(ct);
        }
    }
}
