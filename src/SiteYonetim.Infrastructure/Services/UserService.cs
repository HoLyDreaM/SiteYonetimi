using Microsoft.EntityFrameworkCore;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;
using SiteYonetim.Infrastructure.Data;

namespace SiteYonetim.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly SiteYonetimDbContext _db;

    public UserService(SiteYonetimDbContext db) => _db = db;

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.Users.AsNoTracking()
            .Include(x => x.UserSites).ThenInclude(x => x.Site)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public async Task<IReadOnlyList<User>> GetAllAsync(bool? pendingOnly = null, CancellationToken ct = default)
    {
        IQueryable<User> q = _db.Users.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Include(x => x.UserSites).ThenInclude(x => x.Site);
        if (pendingOnly == true)
            q = q.Where(x => !x.IsApproved);
        return await q.OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
    }

    public async Task ApproveAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted, ct);
        if (user != null)
        {
            user.IsApproved = true;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task DeleteAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted, ct);
        if (user != null)
        {
            user.IsDeleted = true;
            user.IsActive = false;
            await _db.SaveChangesAsync(ct);
        }
    }
}
