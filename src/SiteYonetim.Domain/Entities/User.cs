namespace SiteYonetim.Domain.Entities;

/// <summary>
/// Sistem kullanıcısı (Yönetici, Site yöneticisi vb.)
/// </summary>
public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary>Yönetici onayı. Kayıt sonrası false, onaylanınca true. Onaylanmadan giriş yapamaz.</summary>
    public bool IsApproved { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }

    public ICollection<UserSite> UserSites { get; set; } = new List<UserSite>();
}

public enum UserRole
{
    SuperAdmin = 0,
    SiteManager = 1,
    Accountant = 2,
    Resident = 3
}
