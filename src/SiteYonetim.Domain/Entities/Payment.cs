namespace SiteYonetim.Domain.Entities;

/// <summary>
/// Ödeme / Tahsilat
/// </summary>
public class Payment : BaseEntity
{
    public Guid SiteId { get; set; }
    public Guid ApartmentId { get; set; }
    public Guid? ExpenseShareId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public PaymentMethod Method { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Description { get; set; }
    public Guid? BankAccountId { get; set; }
    public Guid? IncomeId { get; set; }
    public Guid? ReceiptId { get; set; }
    public string? CreditCardLastFour { get; set; }
    public int? InstallmentCount { get; set; }

    public Site Site { get; set; } = null!;
    public Apartment Apartment { get; set; } = null!;
    public ExpenseShare? ExpenseShare { get; set; }
    public BankAccount? BankAccount { get; set; }
    public Income? Income { get; set; }
    public Receipt? Receipt { get; set; }
}

public enum PaymentMethod
{
    Cash = 0,
    BankTransfer = 1,
    CreditCard = 2,
    Check = 3,
    Other = 4
}
