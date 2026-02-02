namespace SiteYonetim.Domain.Entities;

/// <summary>
/// Makbuz
/// </summary>
public class Receipt : BaseEntity
{
    public Guid SiteId { get; set; }
    public Guid PaymentId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string? PdfPath { get; set; }

    public Site Site { get; set; } = null!;
    public Payment Payment { get; set; } = null!;
}
