using SiteYonetim.Domain.Entities;

namespace SiteYonetim.Domain.Interfaces;

public interface IPaymentService
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Payment>> GetBySiteIdAsync(Guid siteId, DateTime? from = null, DateTime? to = null, Guid? apartmentId = null, CancellationToken ct = default);
    Task<IReadOnlyList<Payment>> GetByApartmentIdAsync(Guid apartmentId, CancellationToken ct = default);
    Task<Payment> CreateAsync(Payment payment, CancellationToken ct = default);
    Task<Receipt?> CreateReceiptAsync(Guid paymentId, CancellationToken ct = default);
    /// <summary>Yanlış tahsilatı iptal eder (soft delete). Aidat tekrar tahsil edilebilir hale gelir.</summary>
    Task<bool> DeleteAsync(Guid paymentId, CancellationToken ct = default);
}
