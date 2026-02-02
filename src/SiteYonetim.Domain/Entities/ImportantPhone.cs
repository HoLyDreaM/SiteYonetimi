namespace SiteYonetim.Domain.Entities;

/// <summary>
/// Önemli telefon numaraları (acil, yönetim, vb.)
/// </summary>
public class ImportantPhone : BaseEntity
{
    public Guid SiteId { get; set; }
    /// <summary>İsim / Başlık</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Telefon numarası</summary>
    public string Phone { get; set; } = string.Empty;
    /// <summary>Ek bilgi</summary>
    public string? ExtraInfo { get; set; }

    public Site Site { get; set; } = null!;
}
