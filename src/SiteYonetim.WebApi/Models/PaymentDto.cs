namespace SiteYonetim.WebApi.Models;

public class PaymentDto
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; }
    public Guid ApartmentId { get; set; }
    public string ApartmentNumber { get; set; } = string.Empty;
    public Guid? ExpenseShareId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public int Method { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Description { get; set; }
}

public class CreatePaymentRequest
{
    public Guid SiteId { get; set; }
    public Guid ApartmentId { get; set; }
    public Guid? ExpenseShareId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public int Method { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Description { get; set; }
    public Guid? BankAccountId { get; set; }
    public bool CreateReceipt { get; set; }
}
