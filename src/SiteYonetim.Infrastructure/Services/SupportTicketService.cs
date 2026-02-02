using Microsoft.EntityFrameworkCore;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;
using SiteYonetim.Infrastructure.Data;

namespace SiteYonetim.Infrastructure.Services;

public class SupportTicketService : ISupportTicketService
{
    private readonly SiteYonetimDbContext _db;

    public SupportTicketService(SiteYonetimDbContext db) => _db = db;

    public async Task<SupportTicket?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.SupportTickets.AsNoTracking()
            .Include(x => x.Apartment)
            .Include(x => x.Attachments)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public async Task<IReadOnlyList<SupportTicket>> GetBySiteIdAsync(Guid siteId, CancellationToken ct = default) =>
        await _db.SupportTickets.AsNoTracking()
            .Where(x => x.SiteId == siteId && !x.IsDeleted)
            .Include(x => x.Apartment)
            .Include(x => x.Attachments)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

    public async Task<SupportTicket> CreateAsync(SupportTicket ticket, CancellationToken ct = default)
    {
        _db.SupportTickets.Add(ticket);
        await _db.SaveChangesAsync(ct);
        return ticket;
    }

    public async Task AddAttachmentAsync(SupportTicketAttachment attachment, CancellationToken ct = default)
    {
        _db.SupportTicketAttachments.Add(attachment);
        await _db.SaveChangesAsync(ct);
    }
}
