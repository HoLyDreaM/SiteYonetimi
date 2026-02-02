namespace SiteYonetim.Domain.Entities;

/// <summary>
/// Gider türü (Elektrik, Su, Doğalgaz, Aidat, Asansör bakımı vb.)
/// </summary>
public class ExpenseType : BaseEntity
{
    public Guid SiteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ExpenseShareType ShareType { get; set; } // Eşit pay, Sayaça göre, Özel oran
    public bool IsRecurring { get; set; }
    public int? RecurringDayOfMonth { get; set; } // Ayın kaçında tekrarlanacak
    /// <summary>True ise bu türdeki giderler raporlarda "Toplam Gider"e dahil edilmez (örn: Aidat gelir olduğu için)</summary>
    public bool ExcludeFromReport { get; set; }

    public Site Site { get; set; } = null!;
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<RecurringCharge> RecurringCharges { get; set; } = new List<RecurringCharge>();
}

public enum ExpenseShareType
{
    Equal = 0,      // Eşit pay
    ByMeter = 1,    // Sayaça göre
    ByShareRate = 2, // Aidat pay oranına göre
    Custom = 3      // Özel oran
}
