using SiteYonetim.Domain.Entities;

namespace SiteYonetim.Domain.Interfaces;

public interface ISiteDocumentService
{
    Task<SiteDocument?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<SiteDocument>> GetBySiteIdAsync(Guid siteId, CancellationToken ct = default);
    Task<SiteDocument> CreateAsync(SiteDocument document, CancellationToken ct = default);
    Task<SiteDocument> UpdateAsync(SiteDocument document, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
