using Microsoft.EntityFrameworkCore;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;
using SiteYonetim.Infrastructure.Data;

namespace SiteYonetim.Infrastructure.Services;

public class MeterService : IMeterService
{
    private readonly SiteYonetimDbContext _db;

    public MeterService(SiteYonetimDbContext db) => _db = db;

    public async Task<Meter?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.Meters.AsNoTracking()
            .Include(x => x.Apartment)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public async Task<IReadOnlyList<Meter>> GetBySiteIdAsync(Guid siteId, Guid? apartmentId = null, CancellationToken ct = default)
    {
        var q = _db.Meters.AsNoTracking().Where(x => x.SiteId == siteId && !x.IsDeleted);
        if (apartmentId.HasValue) q = q.Where(x => x.ApartmentId == apartmentId);
        return await q.Include(x => x.Apartment).ToListAsync(ct);
    }

    public async Task<Meter> CreateAsync(Meter meter, CancellationToken ct = default)
    {
        _db.Meters.Add(meter);
        await _db.SaveChangesAsync(ct);
        return meter;
    }

    public async Task<MeterReading> AddReadingAsync(MeterReading reading, CancellationToken ct = default)
    {
        var last = await _db.MeterReadings
            .Where(x => x.MeterId == reading.MeterId && !x.IsDeleted)
            .OrderByDescending(x => x.ReadingDate)
            .FirstOrDefaultAsync(ct);
        if (last != null) reading.PreviousReadingValue = last.ReadingValue;
        _db.MeterReadings.Add(reading);
        await _db.SaveChangesAsync(ct);
        return reading;
    }

    public async Task<IReadOnlyList<MeterReading>> GetReadingsAsync(Guid meterId, DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        var q = _db.MeterReadings.AsNoTracking()
            .Where(x => x.MeterId == meterId && !x.IsDeleted);
        if (from.HasValue) q = q.Where(x => x.ReadingDate >= from.Value);
        if (to.HasValue) q = q.Where(x => x.ReadingDate <= to.Value);
        return await q.OrderByDescending(x => x.ReadingDate).ToListAsync(ct);
    }
}
