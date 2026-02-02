namespace SiteYonetim.Domain.Entities;

/// <summary>
/// Sayaç okuma kaydı
/// </summary>
public class MeterReading : BaseEntity
{
    public Guid MeterId { get; set; }
    public decimal ReadingValue { get; set; }
    public DateTime ReadingDate { get; set; }
    public decimal? PreviousReadingValue { get; set; }
    public decimal? Consumption => PreviousReadingValue.HasValue ? ReadingValue - PreviousReadingValue.Value : null;
    public string? Notes { get; set; }
    public bool IsEstimated { get; set; }

    public Meter Meter { get; set; } = null!;
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
