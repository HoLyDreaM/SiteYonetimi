namespace SiteYonetim.WebApi.Models;

public class ExpenseShareDto
{
    public Guid Id { get; set; }
    public Guid ExpenseId { get; set; }
    public Guid ApartmentId { get; set; }
    public string ApartmentNumber { get; set; } = string.Empty;
    public string? BlockOrBuildingName { get; set; }
    public string ExpenseTypeName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal? LateFeeAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance { get; set; }
    public int Status { get; set; }
    public DateTime? DueDate { get; set; }
}
