using SiteYonetim.Domain.Entities;

namespace SiteYonetim.Domain.Interfaces;

public interface IIncomeService
{
    Task<Income?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Income>> GetBySiteIdAsync(Guid siteId, int? year = null, int? month = null, CancellationToken ct = default);
    Task<decimal> GetTotalIncomeBySiteAsync(Guid siteId, int year, int? month = null, CancellationToken ct = default);
    Task EnsureMonthlyIncomesAsync(int year, int month, CancellationToken ct = default);
    Task<Income> CreateAsync(Income income, CancellationToken ct = default);
    /// <summary>Özel toplama oluşturur - tüm dairelere pay oranına göre dağıtılır (fon, yıllık toplantı kararı vb.)</summary>
    Task CreateExtraCollectionAsync(Guid siteId, int year, int month, decimal totalAmount, string description, CancellationToken ct = default);
    Task<decimal> GetPaidAmountAsync(Guid incomeId, CancellationToken ct = default);
    Task MarkAsPaidAsync(Guid incomeId, Guid paymentId, decimal paidAmount, CancellationToken ct = default);
    /// <summary>Gelir kaydını günceller. Tahsilat yapılmamışsa (paid=0) düzenlenebilir.</summary>
    Task<bool> UpdateAsync(Guid incomeId, decimal amount, string? description, DateTime? paymentStartDate, DateTime? paymentEndDate, DateTime? dueDate, CancellationToken ct = default);
    /// <summary>Gelir kaydını soft-delete ile siler. Tahsilat varsa silinemez.</summary>
    Task<bool> DeleteAsync(Guid incomeId, CancellationToken ct = default);
    /// <summary>Seçilen gelir kayıtlarını siler. Tahsilat yapılmamış olanlar silinir.</summary>
    Task<int> DeleteBulkAsync(IEnumerable<Guid> incomeIds, CancellationToken ct = default);
}
