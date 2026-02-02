using SiteYonetim.Domain.Entities;

namespace SiteYonetim.Domain.Interfaces;

public interface IMeterService
{
    Task<Meter?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Meter>> GetBySiteIdAsync(Guid siteId, Guid? apartmentId = null, CancellationToken ct = default);
    Task<Meter> CreateAsync(Meter meter, CancellationToken ct = default);
    Task<MeterReading> AddReadingAsync(MeterReading reading, CancellationToken ct = default);
    Task<IReadOnlyList<MeterReading>> GetReadingsAsync(Guid meterId, DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
}
