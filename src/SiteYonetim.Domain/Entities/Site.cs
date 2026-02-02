namespace SiteYonetim.Domain.Entities;

/// <summary>
/// Site / Bina / Rezidans
/// </summary>
public class Site : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? TaxOffice { get; set; }
    public string? TaxNumber { get; set; }
    public decimal? LateFeeRate { get; set; } // Gecikme zammı oranı (%)
    public int? LateFeeDay { get; set; } // Kaç gün sonra gecikme zammı uygulanacak
    public bool HasMultipleBlocks { get; set; }
    /// <summary>Varsayılan aylık aidat tutarı (TRY). Dairede tanımlı değilse bu kullanılır.</summary>
    public decimal? DefaultMonthlyDues { get; set; }
    /// <summary>Aidat ödeme dönemi başlangıç günü (1-28, örn: 1 = ayın 1'i)</summary>
    public int DefaultPaymentStartDay { get; set; } = 1;
    /// <summary>Aidat ödeme dönemi bitiş günü (1-28, örn: 20 = ayın 20'si)</summary>
    public int DefaultPaymentEndDay { get; set; } = 20;
    /// <summary>Destek kaydı oluşturulunca bildirim gönderilecek e-posta adresi</summary>
    public string? SupportNotificationEmail { get; set; }
    /// <summary>Destek bildirimi için SMTP sunucusu (örn: smtp.gmail.com)</summary>
    public string? SupportSmtpHost { get; set; }
    /// <summary>SMTP port (örn: 587)</summary>
    public int? SupportSmtpPort { get; set; }
    /// <summary>SMTP kullanıcı adı / e-posta</summary>
    public string? SupportSmtpUsername { get; set; }
    /// <summary>SMTP şifre (şifrelenmiş saklanmalı - basit metin)</summary>
    public string? SupportSmtpPassword { get; set; }

    public ICollection<Building> Buildings { get; set; } = new List<Building>();
    public ICollection<Apartment> Apartments { get; set; } = new List<Apartment>();
    public ICollection<UserSite> UserSites { get; set; } = new List<UserSite>();
    public ICollection<ExpenseType> ExpenseTypes { get; set; } = new List<ExpenseType>();
    public ICollection<BankAccount> BankAccounts { get; set; } = new List<BankAccount>();
    public ICollection<Income> Incomes { get; set; } = new List<Income>();
}
