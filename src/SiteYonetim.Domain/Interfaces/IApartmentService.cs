using SiteYonetim.Domain.Entities;

namespace SiteYonetim.Domain.Interfaces;

public interface IApartmentService
{
    Task<Apartment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Apartment>> GetBySiteIdAsync(Guid siteId, CancellationToken ct = default);
    Task<Apartment> CreateAsync(Apartment apartment, CancellationToken ct = default);
    Task<Apartment> UpdateAsync(Apartment apartment, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
