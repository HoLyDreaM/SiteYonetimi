namespace SiteYonetim.Domain.Entities;

/// <summary>
/// Gider kaydı
/// </summary>
public class Expense : BaseEntity
{
    public Guid SiteId { get; set; }
    public Guid ExpenseTypeId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public DateTime? DueDate { get; set; }
    public ExpenseStatus Status { get; set; }
    public Guid? MeterReadingId { get; set; } // Sayaç okumasına bağlı gider ise
    public string? InvoiceNumber { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public string? Notes { get; set; }

    public Site Site { get; set; } = null!;
    public ExpenseType ExpenseType { get; set; } = null!;
    public MeterReading? MeterReading { get; set; }
    public ICollection<ExpenseShare> ExpenseShares { get; set; } = new List<ExpenseShare>();
    public ICollection<ExpenseAttachment> Attachments { get; set; } = new List<ExpenseAttachment>();
}

public enum ExpenseStatus
{
    Draft = 0,
    Distributed = 1,  // Dairelere dağıtıldı
    PartiallyPaid = 2,
    Paid = 3,
    Cancelled = 4
}
