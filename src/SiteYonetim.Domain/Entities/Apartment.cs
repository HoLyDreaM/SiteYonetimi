namespace SiteYonetim.Domain.Entities;

/// <summary>
/// Daire
/// </summary>
public class Apartment : BaseEntity
{
    public Guid SiteId { get; set; }
    public Guid? BuildingId { get; set; }
    public string BlockOrBuildingName { get; set; } = string.Empty; // Blok adı veya bina adı
    public string ApartmentNumber { get; set; } = string.Empty; // Daire no: 1, 2-A vb.
    public int? Floor { get; set; }
    public decimal ShareRate { get; set; } = 1; // Aidat pay oranı (1 = tam pay)
    /// <summary>Aylık aidat tutarı (TRY). Null ise Site.DefaultMonthlyDues * ShareRate kullanılır.</summary>
    public decimal? MonthlyDuesAmount { get; set; }
    /// <summary>Aidat ödeme başlangıç günü (1-28). Null ise Site.DefaultPaymentStartDay kullanılır.</summary>
    public int? PaymentStartDay { get; set; }
    /// <summary>Aidat ödeme bitiş günü (1-28). Null ise Site.DefaultPaymentEndDay kullanılır.</summary>
    public int? PaymentEndDay { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerPhone { get; set; }
    public string? OwnerEmail { get; set; }
    /// <summary>Ev sahibi mi kiracı mı oturuyor</summary>
    public ApartmentOccupancyType OccupancyType { get; set; } = ApartmentOccupancyType.OwnerOccupied;
    /// <summary>Kiracı adı (kiracı oturuyorsa)</summary>
    public string? TenantName { get; set; }
    /// <summary>Kiracı telefonu</summary>
    public string? TenantPhone { get; set; }

    public Site Site { get; set; } = null!;
    public Building? Building { get; set; }
    public ICollection<Resident> Residents { get; set; } = new List<Resident>();
    public ICollection<ExpenseShare> ExpenseShares { get; set; } = new List<ExpenseShare>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Meter> Meters { get; set; } = new List<Meter>();
    public ICollection<RecurringCharge> RecurringCharges { get; set; } = new List<RecurringCharge>();
    public ICollection<Income> Incomes { get; set; } = new List<Income>();
}

public enum ApartmentOccupancyType
{
    OwnerOccupied = 0,  // Ev sahibi oturuyor
    TenantOccupied = 1  // Kiracı oturuyor
}
