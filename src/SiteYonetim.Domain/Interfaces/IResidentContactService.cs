using SiteYonetim.Domain.Entities;

namespace SiteYonetim.Domain.Interfaces;

public interface IResidentContactService
{
    Task<ResidentContact?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ResidentContact>> GetBySiteIdAsync(Guid siteId, CancellationToken ct = default);
    Task<ResidentContact> CreateAsync(ResidentContact contact, CancellationToken ct = default);
    Task<ResidentContact> UpdateAsync(ResidentContact contact, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
