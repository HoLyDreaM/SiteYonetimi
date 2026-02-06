using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteYonetim.Domain.Interfaces;

namespace SiteYonetim.WebApi.Areas.App.Controllers;

[Area("App")]
[Authorize]
public class ReportsController : Controller
{
    private readonly IReportService _reportService;
    private readonly ISiteService _siteService;

    public ReportsController(IReportService reportService, ISiteService siteService)
    {
        _reportService = reportService;
        _siteService = siteService;
    }

    public async Task<IActionResult> Index(Guid? siteId, CancellationToken ct = default)
    {
        if (!siteId.HasValue)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return RedirectToAction("Login", "Account", new { area = "" });
            ViewBag.Sites = await _siteService.GetUserSitesAsync(userId, ct);
            ViewBag.PageTitle = "Raporlar - Site Seçin";
            return View("SelectSite");
        }
        ViewBag.SiteId = siteId;
        return View();
    }

    public async Task<IActionResult> Monthly(Guid siteId, int year, int month, CancellationToken ct = default)
    {
        var report = await _reportService.GetMonthlyReportDetailAsync(siteId, year, month, ct);
        var site = await _siteService.GetByIdAsync(siteId, ct);
        ViewBag.SiteId = siteId;
        ViewBag.SiteName = site?.Name ?? "";
        ViewBag.Year = year;
        ViewBag.Month = month;
        var monthNames = new[] { "", "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };
        ViewBag.MonthName = monthNames[month];
        return View(report);
    }

    public async Task<IActionResult> MonthlyExcel(Guid siteId, int year, int month, CancellationToken ct = default)
    {
        var report = await _reportService.GetMonthlyReportDetailAsync(siteId, year, month, ct);
        var site = await _siteService.GetByIdAsync(siteId, ct);
        var siteName = site?.Name ?? "Site";
        var monthNames = new[] { "", "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };
        var monthName = monthNames[month];

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Aylık Rapor");

        ws.Cell(1, 1).Value = $"{year} {monthName} - {siteName}";
        ws.Range(1, 1, 1, 6).Merge().Style.Font.Bold = true;
        ws.Cell(2, 1).Value = "";

        var row = 3;
        ws.Cell(row, 1).Value = "GELİRLER";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;
        ws.Cell(row, 1).Value = "Blok/Bina";
        ws.Cell(row, 2).Value = "Daire No";
        ws.Cell(row, 3).Value = "Ev Sahibi";
        ws.Cell(row, 4).Value = "Tür";
        ws.Cell(row, 5).Value = "Tutar";
        ws.Cell(row, 6).Value = "Tahsil Edilen";
        ws.Cell(row, 7).Value = "Kalan";
        ws.Range(row, 1, row, 7).Style.Font.Bold = true;
        row++;

        foreach (var item in report.IncomeItems)
        {
            ws.Cell(row, 1).Value = item.BlockOrBuildingName;
            ws.Cell(row, 2).Value = item.ApartmentNumber;
            ws.Cell(row, 3).Value = item.OwnerName ?? "";
            ws.Cell(row, 4).Value = item.TypeName;
            ws.Cell(row, 5).Value = $"{item.Amount:N2} ₺";
            ws.Cell(row, 6).Value = $"{item.PaidAmount:N2} ₺";
            ws.Cell(row, 7).Value = $"{item.RemainingAmount:N2} ₺";
            row++;
        }
        ws.Cell(row, 1).Value = "AYLIK FİNANS ÖZET";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;
        ws.Cell(row, 1).Value = "Devir Bakiyesi (Bir önceki ayın kapanış bakiyesi)";
        ws.Cell(row, 2).Value = $"{report.OpeningBalance:N2} ₺";
        row++;
        ws.Cell(row, 1).Value = "Tahsil Edilen (Bu ay yapılan tüm tahsilatlar)";
        ws.Cell(row, 2).Value = $"{report.TotalIncome:N2} ₺";
        row++;
        ws.Cell(row, 1).Value = "Bekleyen Aidat ve Diğer Gelirler (Bu ay için - sadece bilgi)";
        ws.Cell(row, 2).Value = $"{report.PendingIncome:N2} ₺";
        row++;
        ws.Cell(row, 1).Value = "Ek Gelir (Bu ay)";
        ws.Cell(row, 2).Value = $"{report.ExtraCollectionIncome:N2} ₺";
        row++;
        ws.Cell(row, 1).Value = "Toplam Gider (Sadece bu ayın giderleri)";
        ws.Cell(row, 2).Value = $"{report.TotalExpense:N2} ₺";
        row++;
        ws.Cell(row, 1).Value = "Bakiye (Devir + Tahsil - Gider)";
        ws.Cell(row, 2).Value = $"{report.Balance:N2} ₺";
        ws.Range(row - 6, 1, row, 1).Style.Font.Bold = true;
        row += 2;

        ws.Cell(row, 1).Value = "GİDERLER";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;
        ws.Cell(row, 1).Value = "Gider Türü";
        ws.Cell(row, 2).Value = "Açıklama";
        ws.Cell(row, 3).Value = "Tarih";
        ws.Cell(row, 4).Value = "Fatura No";
        ws.Cell(row, 5).Value = "Tutar";
        ws.Range(row, 1, row, 5).Style.Font.Bold = true;
        row++;

        foreach (var item in report.ExpenseItems)
        {
            ws.Cell(row, 1).Value = item.ExpenseTypeName;
            ws.Cell(row, 2).Value = item.Description;
            ws.Cell(row, 3).Value = item.ExpenseDate.ToString("dd.MM.yyyy");
            ws.Cell(row, 4).Value = item.InvoiceNumber ?? "";
            ws.Cell(row, 5).Value = $"{item.Amount:N2} ₺";
            row++;
        }
        ws.Cell(row, 1).Value = "TOPLAM GİDER";
        ws.Cell(row, 5).Value = $"{report.TotalExpense:N2} ₺";
        ws.Range(row, 1, row, 5).Style.Font.Bold = true;
        row += 2;

        ws.Cell(row, 1).Value = "GENEL TOPLAM";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = $"Bakiye (Devir + Tahsil - Gider): {report.Balance:N2} ₺";
        row++;

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;
        var fileName = $"AylikRapor_{siteName}_{year}_{month}.xlsx".Replace(" ", "_");
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    public async Task<IActionResult> Yearly(Guid siteId, int year, CancellationToken ct = default)
    {
        if (year < 2000 || year > 2100)
            year = DateTime.Today.Year;
        var report = await _reportService.GetYearlyReportDetailAsync(siteId, year, ct);
        var site = await _siteService.GetByIdAsync(siteId, ct);
        ViewBag.SiteId = siteId;
        ViewBag.SiteName = site?.Name ?? "";
        ViewBag.Year = year;
        return View(report);
    }

    public async Task<IActionResult> YearlyExcel(Guid siteId, int year, CancellationToken ct = default)
    {
        if (year < 2000 || year > 2100)
            year = DateTime.Today.Year;
        var report = await _reportService.GetYearlyReportDetailAsync(siteId, year, ct);
        var site = await _siteService.GetByIdAsync(siteId, ct);
        var siteName = site?.Name ?? "Site";
        var monthNames = new[] { "", "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Yıllık Rapor");

        ws.Cell(1, 1).Value = $"{year} - {siteName}";
        ws.Range(1, 1, 1, 7).Merge().Style.Font.Bold = true;
        ws.Cell(2, 1).Value = "";

        var row = 3;
        ws.Cell(row, 1).Value = "YILLIK FİNANS ÖZET";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;
        ws.Cell(row, 1).Value = "Önceki Yıllar Devir Bakiyesi (Açılış)";
        ws.Cell(row, 2).Value = $"{report.OpeningBalance:N2} ₺";
        row++;
        ws.Cell(row, 1).Value = $"Tahsil Edilen ({year})";
        ws.Cell(row, 2).Value = $"{report.TotalIncome:N2} ₺";
        row++;
        ws.Cell(row, 1).Value = "Bekleyen Aidat ve Diğer Gelirler (Bu yıl için - sadece bilgi)";
        ws.Cell(row, 2).Value = $"{report.PendingIncome:N2} ₺";
        row++;
        ws.Cell(row, 1).Value = "Yıllık Toplam Gider";
        ws.Cell(row, 2).Value = $"{report.TotalExpense:N2} ₺";
        row++;
        ws.Cell(row, 1).Value = "Yıllık Bakiye (Devir + Tahsil - Gider)";
        ws.Cell(row, 2).Value = $"{report.Balance:N2} ₺";
        ws.Range(row - 5, 1, row, 1).Style.Font.Bold = true;
        row += 2;

        ws.Cell(row, 1).Value = "AYLIK ÖZET";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;
        ws.Cell(row, 1).Value = "Ay";
        ws.Cell(row, 2).Value = "Devir";
        ws.Cell(row, 3).Value = "Gelir";
        ws.Cell(row, 4).Value = "Gider";
        ws.Cell(row, 5).Value = "Bakiye";
        ws.Range(row, 1, row, 5).Style.Font.Bold = true;
        row++;

        foreach (var m in report.ByMonth)
        {
            ws.Cell(row, 1).Value = monthNames[m.Month];
            ws.Cell(row, 2).Value = $"{m.OpeningBalance:N2} ₺";
            ws.Cell(row, 3).Value = $"{m.TotalIncome:N2} ₺";
            ws.Cell(row, 4).Value = $"{m.TotalExpense:N2} ₺";
            ws.Cell(row, 5).Value = $"{m.Balance:N2} ₺";
            row++;
        }

        ws.Cell(row, 1).Value = "TOPLAM";
        ws.Cell(row, 2).Value = "";
        ws.Cell(row, 3).Value = $"{report.TotalIncome:N2} ₺";
        ws.Cell(row, 4).Value = $"{report.TotalExpense:N2} ₺";
        ws.Cell(row, 5).Value = $"{report.Balance:N2} ₺";
        ws.Range(row, 1, row, 5).Style.Font.Bold = true;
        row += 2;

        foreach (var monthDetail in report.ByMonthDetail)
        {
            var monthName = monthNames[monthDetail.Month];
            ws.Cell(row, 1).Value = $"{monthName} - GELİRLER";
            ws.Cell(row, 1).Style.Font.Bold = true;
            row++;
            ws.Cell(row, 1).Value = "Blok/Bina";
            ws.Cell(row, 2).Value = "Daire No";
            ws.Cell(row, 3).Value = "Ev Sahibi";
            ws.Cell(row, 4).Value = "Tür";
            ws.Cell(row, 5).Value = "Tutar";
            ws.Cell(row, 6).Value = "Tahsil Edilen";
            ws.Cell(row, 7).Value = "Kalan";
            ws.Range(row, 1, row, 7).Style.Font.Bold = true;
            row++;

            foreach (var item in monthDetail.IncomeItems)
            {
                ws.Cell(row, 1).Value = item.BlockOrBuildingName;
                ws.Cell(row, 2).Value = item.ApartmentNumber;
                ws.Cell(row, 3).Value = item.OwnerName ?? "";
                ws.Cell(row, 4).Value = item.TypeName;
                ws.Cell(row, 5).Value = $"{item.Amount:N2} ₺";
                ws.Cell(row, 6).Value = $"{item.PaidAmount:N2} ₺";
                ws.Cell(row, 7).Value = $"{item.RemainingAmount:N2} ₺";
                row++;
            }
            ws.Cell(row, 4).Value = "TOPLAM GELİR (Tahsil)";
            ws.Cell(row, 6).Value = $"{monthDetail.TotalIncome:N2} ₺";
            ws.Range(row, 4, row, 6).Style.Font.Bold = true;
            row++;
            ws.Cell(row, 4).Value = "BEKLEYEN GELİR";
            ws.Cell(row, 6).Value = $"{monthDetail.PendingIncome:N2} ₺";
            ws.Range(row, 4, row, 6).Style.Font.Bold = true;
            row += 2;

            ws.Cell(row, 1).Value = $"{monthName} - GİDERLER";
            ws.Cell(row, 1).Style.Font.Bold = true;
            row++;
            ws.Cell(row, 1).Value = "Gider Türü";
            ws.Cell(row, 2).Value = "Açıklama";
            ws.Cell(row, 3).Value = "Tarih";
            ws.Cell(row, 4).Value = "Fatura No";
            ws.Cell(row, 5).Value = "Tutar";
            ws.Range(row, 1, row, 5).Style.Font.Bold = true;
            row++;

            foreach (var item in monthDetail.ExpenseItems)
            {
                ws.Cell(row, 1).Value = item.ExpenseTypeName;
                ws.Cell(row, 2).Value = item.Description;
                ws.Cell(row, 3).Value = item.ExpenseDate.ToString("dd.MM.yyyy");
                ws.Cell(row, 4).Value = item.InvoiceNumber ?? "";
                ws.Cell(row, 5).Value = $"{item.Amount:N2} ₺";
                row++;
            }
            ws.Cell(row, 1).Value = "TOPLAM GİDER";
            ws.Cell(row, 5).Value = $"{monthDetail.TotalExpense:N2} ₺";
            ws.Range(row, 1, row, 5).Style.Font.Bold = true;
            row += 2;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;
        var fileName = $"YillikRapor_{siteName}_{year}.xlsx".Replace(" ", "_");
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    public async Task<IActionResult> HazirunCetveli(Guid siteId, [FromQuery] DateTime? date, CancellationToken ct = default)
    {
        var report = await _reportService.GetHazirunCetveliAsync(siteId, date ?? DateTime.Today, ct);
        var site = await _siteService.GetByIdAsync(siteId, ct);
        ViewBag.SiteId = siteId;
        ViewBag.SiteName = site?.Name ?? "";
        return View(report);
    }

    public async Task<IActionResult> HazirunCetveliPrint(Guid siteId, [FromQuery] DateTime? date, CancellationToken ct = default)
    {
        var report = await _reportService.GetHazirunCetveliAsync(siteId, date ?? DateTime.Today, ct);
        ViewBag.SiteId = siteId;
        return View(report);
    }
}
