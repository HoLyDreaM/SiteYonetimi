namespace SiteYonetim.Domain.Entities;

/// <summary>
/// Evrak arşivi - Sözleşme, belge vb. (PDF, Word, Excel)
/// </summary>
public class SiteDocument : BaseEntity
{
    public Guid SiteId { get; set; }
    /// <summary>Evrak adı</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Açıklama</summary>
    public string? Description { get; set; }
    /// <summary>Dosya yolu (uploads/evraklar/{siteId}/...)</summary>
    public string? FilePath { get; set; }
    /// <summary>Orijinal dosya adı</summary>
    public string? FileName { get; set; }

    public Site Site { get; set; } = null!;
}
