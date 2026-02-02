using SiteYonetim.Domain.Entities;

namespace SiteYonetim.Domain.Interfaces;

public interface IImportantPhoneService
{
    Task<ImportantPhone?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ImportantPhone>> GetBySiteIdAsync(Guid siteId, CancellationToken ct = default);
    Task<ImportantPhone> CreateAsync(ImportantPhone phone, CancellationToken ct = default);
    Task<ImportantPhone> UpdateAsync(ImportantPhone phone, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
