using SiteYonetim.Domain.Entities;

namespace SiteYonetim.Domain.Interfaces;

public interface ISiteService
{
    Task<Site?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Site>> GetUserSitesAsync(Guid userId, CancellationToken ct = default);
    Task<Site> CreateAsync(Site site, Guid? ownerUserId = null, CancellationToken ct = default);
    Task<Site> UpdateAsync(Site site, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
