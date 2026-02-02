namespace SiteYonetim.Domain.Entities;

/// <summary>
/// Teklif - Firma teklifleri (PDF, Word, Excel vb.)
/// </summary>
public class Quotation : BaseEntity
{
    public Guid SiteId { get; set; }
    /// <summary>Teklifi veren firma adı</summary>
    public string CompanyName { get; set; } = string.Empty;
    /// <summary>Teklif tarihi</summary>
    public DateTime QuotationDate { get; set; }
    /// <summary>Dosya yolu (PDF, Word, Excel vb.)</summary>
    public string? FilePath { get; set; }
    /// <summary>Aylık ücret (TRY)</summary>
    public decimal? MonthlyFee { get; set; }
    /// <summary>Yıllık ücret (TRY)</summary>
    public decimal? YearlyFee { get; set; }
    /// <summary>Açıklama</summary>
    public string? Description { get; set; }

    public Site Site { get; set; } = null!;
}
