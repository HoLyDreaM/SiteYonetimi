namespace SiteYonetim.Domain.Entities;

/// <summary>
/// Kat malikleri ve kiracıların telefon bilgileri
/// </summary>
public class ResidentContact : BaseEntity
{
    public Guid SiteId { get; set; }
    public Guid? ApartmentId { get; set; }
    /// <summary>Ad Soyad</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Telefon numarası</summary>
    public string Phone { get; set; } = string.Empty;
    /// <summary>Malik veya Kiracı</summary>
    public ResidentContactType ContactType { get; set; }
    /// <summary>Ek notlar</summary>
    public string? Notes { get; set; }

    public Site Site { get; set; } = null!;
    public Apartment? Apartment { get; set; }
}

public enum ResidentContactType
{
    Owner = 0,   // Kat maliki
    Tenant = 1   // Kiracı
}
