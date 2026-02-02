namespace SiteYonetim.Domain.Entities;

/// <summary>
/// Tekrarlayan aidat/borç tanımı (Aylık aidat vb.)
/// </summary>
public class RecurringCharge : BaseEntity
{
    public Guid SiteId { get; set; }
    public Guid ApartmentId { get; set; }
    public Guid ExpenseTypeId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int DayOfMonth { get; set; } // 1-28
    public bool IsActive { get; set; } = true;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public Site Site { get; set; } = null!;
    public Apartment Apartment { get; set; } = null!;
    public ExpenseType ExpenseType { get; set; } = null!;
}
