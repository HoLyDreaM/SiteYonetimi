namespace SiteYonetim.Domain.Entities;

/// <summary>
/// Site banka hesabı
/// </summary>
public class BankAccount : BaseEntity
{
    public Guid SiteId { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string IBAN { get; set; } = string.Empty;
    public string? AccountName { get; set; }
    public string? Currency { get; set; } = "TRY";
    public bool IsDefault { get; set; }
    /// <summary>Başlangıç bakiyesi (TRY). Kullanıcının girdiği ilk bakiye, değişmez.</summary>
    public decimal OpeningBalance { get; set; }
    /// <summary>Güncel bakiye (TRY). Tahsilatlar artırır, gider ödemeleri azaltır. Mutabakat için güncellenebilir.</summary>
    public decimal CurrentBalance { get; set; }

    public Site Site { get; set; } = null!;
    public ICollection<BankTransaction> Transactions { get; set; } = new List<BankTransaction>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
