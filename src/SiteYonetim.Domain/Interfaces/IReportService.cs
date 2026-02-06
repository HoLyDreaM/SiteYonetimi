namespace SiteYonetim.Domain.Interfaces;

public class MonthlyReportDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    /// <summary>Önceki dönemlerden devreden bakiye (ay başı kasa)</summary>
    public decimal OpeningBalance { get; set; }
    /// <summary>Tahsil edilen gelir (aidat + ek gelir) - bu ay</summary>
    public decimal TotalIncome { get; set; }
    /// <summary>Tahsil edilmemiş, bekleyen gelir</summary>
    public decimal PendingIncome { get; set; }
    /// <summary>Ek gelir (Özel Toplama) tahsil edilen - bu ay</summary>
    public decimal ExtraCollectionIncome { get; set; }
    /// <summary>Ek gelir (Özel Toplama) bekleyen</summary>
    public decimal ExtraCollectionPending { get; set; }
    public decimal TotalExpense { get; set; }
    /// <summary>Bakiye = Devir Bakiyesi + Tahsil Edilen - Toplam Gider</summary>
    public decimal Balance => OpeningBalance + TotalIncome - TotalExpense;
}

/// <summary>Kalem kalem gelir satırı</summary>
public class MonthlyReportIncomeItemDto
{
    public string BlockOrBuildingName { get; set; } = string.Empty;
    public string ApartmentNumber { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount => Amount - PaidAmount;
    public DateTime DueDate { get; set; }
}

/// <summary>Kalem kalem gider satırı</summary>
public class MonthlyReportExpenseItemDto
{
    public string ExpenseTypeName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string? InvoiceNumber { get; set; }
}

/// <summary>Detaylı aylık rapor (kalem kalem gelir/gider listesi)</summary>
public class MonthlyReportDetailDto : MonthlyReportDto
{
    public IReadOnlyList<MonthlyReportIncomeItemDto> IncomeItems { get; set; } = Array.Empty<MonthlyReportIncomeItemDto>();
    public IReadOnlyList<MonthlyReportExpenseItemDto> ExpenseItems { get; set; } = Array.Empty<MonthlyReportExpenseItemDto>();
}

public class YearlyReportDto
{
    public int Year { get; set; }
    /// <summary>Tahsil edilen gelir</summary>
    public decimal TotalIncome { get; set; }
    /// <summary>Tahsil edilmemiş, bekleyen gelir</summary>
    public decimal PendingIncome { get; set; }
    /// <summary>Ek gelir (Özel Toplama) tahsil edilen</summary>
    public decimal ExtraCollectionIncome { get; set; }
    /// <summary>Ek gelir (Özel Toplama) bekleyen</summary>
    public decimal ExtraCollectionPending { get; set; }
    public decimal TotalExpense { get; set; }
    /// <summary>Yıllık Bakiye = Önceki Yıllar Devir + Tahsil Edilen - Yıllık Toplam Gider</summary>
    public decimal Balance => OpeningBalance + TotalIncome - TotalExpense;
    public IReadOnlyList<MonthlyReportDto> ByMonth { get; set; } = Array.Empty<MonthlyReportDto>();
    /// <summary>Önceki Yıllar Devir Bakiyesi (Açılış)</summary>
    public decimal OpeningBalance { get; set; }
}

/// <summary>Detaylı yıllık rapor (her ay için kalem kalem gelir/gider)</summary>
public class YearlyReportDetailDto : YearlyReportDto
{
    public IReadOnlyList<MonthlyReportDetailDto> ByMonthDetail { get; set; } = Array.Empty<MonthlyReportDetailDto>();
}

public class DebtorDto
{
    public Guid ApartmentId { get; set; }
    public string BlockOrBuildingName { get; set; } = string.Empty;
    public string ApartmentNumber { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
    public string? OwnerPhone { get; set; }
    public decimal UnpaidExpenseShare { get; set; }
    public decimal UnpaidIncome { get; set; }
    public decimal TotalDebt => UnpaidExpenseShare + UnpaidIncome;
    /// <summary>En eski borç vade tarihi</summary>
    public DateTime? OldestDebtDate { get; set; }
    /// <summary>Kaç gün gecikmiş (vade geçmişse)</summary>
    public int? DaysOverdue { get; set; }
}

/// <summary>Belirli dönem için tahsilat, gider ve kasa toplamları. Referans doğrulama için.</summary>
public class PeriodVerificationDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalTahsilat { get; set; }
    public decimal TotalGider { get; set; }
    /// <summary>KASA = TOPLAM TAHSİLAT - TOPLAM GİDER</summary>
    public decimal Kasa => TotalTahsilat - TotalGider;
}

public interface IReportService
{
    Task<MonthlyReportDto> GetMonthlyReportAsync(Guid siteId, int year, int month, CancellationToken ct = default);
    Task<MonthlyReportDetailDto> GetMonthlyReportDetailAsync(Guid siteId, int year, int month, CancellationToken ct = default);
    Task<YearlyReportDto> GetYearlyReportAsync(Guid siteId, int year, CancellationToken ct = default);
    Task<YearlyReportDetailDto> GetYearlyReportDetailAsync(Guid siteId, int year, CancellationToken ct = default);
    Task<PeriodVerificationDto> GetPeriodVerificationAsync(Guid siteId, DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task<IReadOnlyList<DebtorDto>> GetDebtorsAsync(Guid siteId, CancellationToken ct = default);
    Task<HazirunCetveliDto> GetHazirunCetveliAsync(Guid siteId, DateTime? date = null, CancellationToken ct = default);
}

/// <summary>Hazırün cetveli - toplantı katılım listesi</summary>
public class HazirunCetveliDto
{
    public string SiteName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public IReadOnlyList<HazirunCetveliItemDto> Items { get; set; } = Array.Empty<HazirunCetveliItemDto>();
}

public class HazirunCetveliItemDto
{
    public string BlockOrBuildingName { get; set; } = string.Empty;
    public string ApartmentNumber { get; set; } = string.Empty;
    /// <summary>Kat maliki adı (ev sahibi)</summary>
    public string KatMaliki { get; set; } = string.Empty;
    /// <summary>Varsa vekil adı (toplantıda temsil eden kişi - manuel doldurulabilir)</summary>
    public string? VekilAdi { get; set; }
}
