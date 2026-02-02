using SiteYonetim.Domain.Entities;

namespace SiteYonetim.Domain.Interfaces;

public interface ISupportTicketService
{
    Task<SupportTicket?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<SupportTicket>> GetBySiteIdAsync(Guid siteId, CancellationToken ct = default);
    Task<SupportTicket> CreateAsync(SupportTicket ticket, CancellationToken ct = default);
    Task AddAttachmentAsync(SupportTicketAttachment attachment, CancellationToken ct = default);
}
