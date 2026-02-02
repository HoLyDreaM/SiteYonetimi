namespace SiteYonetim.WebApi.Models;

public class ExpenseDto
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; }
    public Guid ExpenseTypeId { get; set; }
    public string ExpenseTypeName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public DateTime? DueDate { get; set; }
    public int Status { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? Notes { get; set; }
}

public class CreateExpenseRequest
{
    public Guid SiteId { get; set; }
    public Guid ExpenseTypeId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? Notes { get; set; }
}
