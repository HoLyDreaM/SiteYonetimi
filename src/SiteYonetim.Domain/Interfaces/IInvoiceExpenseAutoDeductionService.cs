namespace SiteYonetim.Domain.Interfaces;

/// <summary>
/// Fatura tarihi gelen giderleri banka hesabından otomatik düşer.
/// </summary>
public interface IInvoiceExpenseAutoDeductionService
{
    Task ProcessDueExpensesAsync(CancellationToken ct = default);
}
