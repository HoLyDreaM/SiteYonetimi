namespace SiteYonetim.Domain.Interfaces;

/// <summary>
/// Ödemesi gelen (bankadan düşülmüş) giderler için bildirim verisi
/// </summary>
public class PaidExpenseNotificationDto
{
    public Guid ExpenseId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ExpenseTypeName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
}

public interface IPaidExpenseNotificationService
{
    /// <summary>
    /// Son 30 gün içinde bankadan ödenen (BankTransaction oluşturulmuş) giderleri getirir
    /// </summary>
    Task<IReadOnlyList<PaidExpenseNotificationDto>> GetRecentlyPaidExpensesAsync(Guid siteId, int lastDays = 30, CancellationToken ct = default);
}
