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

/// <summary>
/// Süresi geçmiş aidat ödemeleri için bildirim verisi
/// </summary>
public class OverdueAidatNotificationDto
{
    public Guid IncomeId { get; set; }
    public string ApartmentInfo { get; set; } = string.Empty;
    public string BlockOrBuilding { get; set; } = string.Empty;
    public string ApartmentNumber { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal RemainingAmount { get; set; }
    public DateTime PaymentEndDate { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
}

public interface IPaidExpenseNotificationService
{
    /// <summary>
    /// Son 30 gün içinde bankadan ödenen (BankTransaction oluşturulmuş) giderleri getirir
    /// </summary>
    Task<IReadOnlyList<PaidExpenseNotificationDto>> GetRecentlyPaidExpensesAsync(Guid siteId, int lastDays = 30, CancellationToken ct = default);

    /// <summary>
    /// Ödeme süresi geçmiş ve tahsil edilmemiş/kısmi tahsil edilmiş aidatları getirir
    /// </summary>
    Task<IReadOnlyList<OverdueAidatNotificationDto>> GetOverdueAidatAsync(Guid siteId, CancellationToken ct = default);
}
