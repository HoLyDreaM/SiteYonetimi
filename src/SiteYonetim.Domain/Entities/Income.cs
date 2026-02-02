namespace SiteYonetim.Domain.Entities;

/// <summary>
/// Gelir kaydı (Aidat vb. - dairelerden tahsil edilecek)
/// </summary>
public class Income : BaseEntity
{
    public Guid SiteId { get; set; }
    public Guid ApartmentId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Amount { get; set; }
    public IncomeType Type { get; set; } = IncomeType.Aidat;
    public IncomeStatus Status { get; set; } = IncomeStatus.Unpaid;
    public Guid? PaymentId { get; set; }
    public DateTime DueDate { get; set; }
    /// <summary>Ödeme başlangıç tarihi (örn: ayın 1'i)</summary>
    public DateTime PaymentStartDate { get; set; }
    /// <summary>Ödeme bitiş tarihi (örn: ayın 20'si)</summary>
    public DateTime PaymentEndDate { get; set; }
    public string? Description { get; set; }

    public Site Site { get; set; } = null!;
    public Apartment Apartment { get; set; } = null!;
    public Payment? Payment { get; set; }
}

public enum IncomeType
{
    Aidat = 0,
    Other = 1
}

public enum IncomeStatus
{
    Unpaid = 0,
    Paid = 1,
    PartiallyPaid = 2
}
