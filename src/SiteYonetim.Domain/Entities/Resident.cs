namespace SiteYonetim.Domain.Entities;

/// <summary>
/// Daire sakini (Malik veya Kiracı)
/// </summary>
public class Resident : BaseEntity
{
    public Guid ApartmentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public ResidentType Type { get; set; } // Malik, Kiracı
    public bool IsOwner { get; set; }
    public string? IdentityNumber { get; set; }
    public DateTime? MoveInDate { get; set; }
    public DateTime? MoveOutDate { get; set; }
    public Guid? UserId { get; set; } // Giriş yapabiliyorsa

    public Apartment Apartment { get; set; } = null!;
    public User? User { get; set; }
}

public enum ResidentType
{
    Owner = 0,
    Tenant = 1
}
