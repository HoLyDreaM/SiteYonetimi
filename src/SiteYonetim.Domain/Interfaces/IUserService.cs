using SiteYonetim.Domain.Entities;

namespace SiteYonetim.Domain.Interfaces;

public interface IUserService
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetAllAsync(bool? pendingOnly = null, CancellationToken ct = default);
    Task ApproveAsync(Guid userId, CancellationToken ct = default);
    Task DeleteAsync(Guid userId, CancellationToken ct = default);
}
