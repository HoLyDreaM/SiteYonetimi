using System.ComponentModel.DataAnnotations.Schema;

namespace SiteYonetim.Domain.Entities;

/// <summary>
/// Gider paylaşımı - Daireye düşen borç
/// </summary>
public class ExpenseShare : BaseEntity
{
    public Guid ExpenseId { get; set; }
    public Guid ApartmentId { get; set; }
    public decimal Amount { get; set; }
    public decimal? LateFeeAmount { get; set; }
    [NotMapped] public decimal TotalAmount => Amount + (LateFeeAmount ?? 0);
    public decimal PaidAmount { get; set; }
    [NotMapped] public decimal Balance => TotalAmount - PaidAmount;
    public ExpenseShareStatus Status { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Notes { get; set; }

    public Expense Expense { get; set; } = null!;
    public Apartment Apartment { get; set; } = null!;
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public enum ExpenseShareStatus
{
    Pending = 0,
    PartiallyPaid = 1,
    Paid = 2,
    Overdue = 3,
    Cancelled = 4
}
