using SiteYonetim.Domain.Entities;

namespace SiteYonetim.Domain.Interfaces;

public interface IQuotationService
{
    Task<Quotation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Quotation>> GetBySiteIdAsync(Guid siteId, CancellationToken ct = default);
    Task<Quotation> CreateAsync(Quotation quotation, CancellationToken ct = default);
    Task<Quotation> UpdateAsync(Quotation quotation, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
