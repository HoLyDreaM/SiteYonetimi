namespace SiteYonetim.Domain.Entities;

/// <summary>
/// Gider faturası ek dosyası (JPG, PDF)
/// </summary>
public class ExpenseAttachment : BaseEntity
{
    public Guid ExpenseId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;

    public Expense Expense { get; set; } = null!;
}
