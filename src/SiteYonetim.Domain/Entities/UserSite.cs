namespace SiteYonetim.Domain.Entities;

/// <summary>
/// Kullanıcı-Site ilişkisi (Bir kullanıcı birden fazla sitede yönetici olabilir)
/// </summary>
public class UserSite : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid SiteId { get; set; }
    public bool IsPrimary { get; set; }

    public User User { get; set; } = null!;
    public Site Site { get; set; } = null!;
}
