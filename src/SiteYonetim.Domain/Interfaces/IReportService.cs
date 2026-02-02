namespace SiteYonetim.Domain.Interfaces;

public class MonthlyReportDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    /// <summary>Tahsil edilen gelir (aidat vb.)</summary>
    public decimal TotalIncome { get; set; }
    /// <summary>Tahsil edilmemiş, bekleyen gelir</summary>
    public decimal PendingIncome { get; set; }
    public decimal TotalExpense { get; set; }
    /// <summary>Bakiye: (Tahsil + Bekleyen aidat) - Gider</summary>
    public decimal Balance => (TotalIncome + PendingIncome) - TotalExpense;
}

public class YearlyReportDto
{
    public int Year { get; set; }
    /// <summary>Tahsil edilen gelir</summary>
    public decimal TotalIncome { get; set; }
    /// <summary>Tahsil edilmemiş, bekleyen gelir</summary>
    public decimal PendingIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal Balance => (TotalIncome + PendingIncome) - TotalExpense;
    public IReadOnlyList<MonthlyReportDto> ByMonth { get; set; } = Array.Empty<MonthlyReportDto>();
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

public interface IReportService
{
    Task<MonthlyReportDto> GetMonthlyReportAsync(Guid siteId, int year, int month, CancellationToken ct = default);
    Task<YearlyReportDto> GetYearlyReportAsync(Guid siteId, int year, CancellationToken ct = default);
    Task<IReadOnlyList<DebtorDto>> GetDebtorsAsync(Guid siteId, CancellationToken ct = default);
}
