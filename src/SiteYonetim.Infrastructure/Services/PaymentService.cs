using Microsoft.EntityFrameworkCore;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;
using SiteYonetim.Infrastructure.Data;

namespace SiteYonetim.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly SiteYonetimDbContext _db;

    public PaymentService(SiteYonetimDbContext db) => _db = db;

    public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.Payments.AsNoTracking()
            .Include(x => x.Apartment)
            .Include(x => x.ExpenseShare).ThenInclude(e => e!.Expense).ThenInclude(e => e!.ExpenseType)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public async Task<IReadOnlyList<Payment>> GetBySiteIdAsync(Guid siteId, DateTime? from = null, DateTime? to = null, Guid? apartmentId = null, CancellationToken ct = default)
    {
        IQueryable<Payment> q = _db.Payments.AsNoTracking()
            .Where(x => x.SiteId == siteId && !x.IsDeleted)
            .Include(x => x.Apartment);
        if (from.HasValue) q = q.Where(x => x.PaymentDate >= from.Value);
        if (to.HasValue) q = q.Where(x => x.PaymentDate <= to.Value);
        if (apartmentId.HasValue) q = q.Where(x => x.ApartmentId == apartmentId.Value);
        return await q.OrderByDescending(x => x.PaymentDate).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Payment>> GetByApartmentIdAsync(Guid apartmentId, CancellationToken ct = default) =>
        await _db.Payments.AsNoTracking()
            .Where(x => x.ApartmentId == apartmentId && !x.IsDeleted)
            .OrderByDescending(x => x.PaymentDate)
            .ToListAsync(ct);

    public async Task<Payment> CreateAsync(Payment payment, CancellationToken ct = default)
    {
        _db.Payments.Add(payment);
        if (payment.ExpenseShareId.HasValue)
        {
            var share = await _db.ExpenseShares.FirstOrDefaultAsync(x => x.Id == payment.ExpenseShareId && !x.IsDeleted, ct);
            if (share != null)
            {
                share.PaidAmount += payment.Amount;
                share.Status = share.PaidAmount >= share.Amount + (share.LateFeeAmount ?? 0)
                    ? ExpenseShareStatus.Paid
                    : ExpenseShareStatus.PartiallyPaid;
            }
        }
        await _db.SaveChangesAsync(ct); // Payment.Id atanır

        if (payment.BankAccountId.HasValue)
        {
            var bank = await _db.BankAccounts.FirstOrDefaultAsync(x => x.Id == payment.BankAccountId && !x.IsDeleted, ct);
            if (bank != null)
            {
                bank.CurrentBalance += payment.Amount;
                _db.BankTransactions.Add(new BankTransaction
                {
                    BankAccountId = bank.Id,
                    TransactionDate = payment.PaymentDate,
                    Amount = payment.Amount,
                    Type = TransactionType.Income,
                    Description = $"Tahsilat: {payment.Description ?? "Aidat ödemesi"}",
                    PaymentId = payment.Id,
                    BalanceAfter = bank.CurrentBalance,
                    IsDeleted = false
                });
                await _db.SaveChangesAsync(ct);
            }
        }
        return payment;
    }

    public async Task<Receipt?> CreateReceiptAsync(Guid paymentId, CancellationToken ct = default)
    {
        var payment = await _db.Payments.Include(x => x.Site).FirstOrDefaultAsync(x => x.Id == paymentId && !x.IsDeleted, ct);
        if (payment == null) return null;

        var lastNo = await _db.Receipts
            .Where(x => x.SiteId == payment.SiteId && !x.IsDeleted)
            .OrderByDescending(x => x.ReceiptNumber)
            .Select(x => x.ReceiptNumber)
            .FirstOrDefaultAsync(ct);
        var nextNo = string.IsNullOrEmpty(lastNo) ? "1" : (int.Parse(lastNo) + 1).ToString();

        var receipt = new Receipt
        {
            SiteId = payment.SiteId,
            PaymentId = payment.Id,
            ReceiptNumber = nextNo,
            ReceiptDate = DateTime.UtcNow,
            Amount = payment.Amount,
            Description = $"Tahsilat - {payment.PaymentDate:dd.MM.yyyy}"
        };
        _db.Receipts.Add(receipt);
        await _db.SaveChangesAsync(ct);
        payment.ReceiptId = receipt.Id;
        await _db.SaveChangesAsync(ct);
        return receipt;
    }
}
