namespace SiteYonetim.Domain.Entities;

/// <summary>
/// Banka hareketi (Banka entegrasyonu veya manuel giriş)
/// </summary>
public class BankTransaction : BaseEntity
{
    public Guid BankAccountId { get; set; }
    public DateTime TransactionDate { get; set; }
    public decimal Amount { get; set; } // Pozitif = giriş, negatif = çıkış
    public TransactionType Type { get; set; }
    public string? Description { get; set; }
    public string? ReferenceNumber { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? ExpenseId { get; set; }
    public decimal BalanceAfter { get; set; }

    public BankAccount BankAccount { get; set; } = null!;
    public Payment? Payment { get; set; }
    public Expense? Expense { get; set; }
}

public enum TransactionType
{
    Income = 0,
    Expense = 1,
    Transfer = 2
}
