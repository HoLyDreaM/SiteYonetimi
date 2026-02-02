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
        var report = await _reportService.GetMonthlyReportAsync(siteId, year, month, ct);
        var site = await _siteService.GetByIdAsync(siteId, ct);
        ViewBag.SiteId = siteId;
        ViewBag.SiteName = site?.Name ?? "";
        ViewBag.Year = year;
        ViewBag.Month = month;
        var monthNames = new[] { "", "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };
        ViewBag.MonthName = monthNames[month];
        return View(report);
    }

    public async Task<IActionResult> Yearly(Guid siteId, int year, CancellationToken ct = default)
    {
        var report = await _reportService.GetYearlyReportAsync(siteId, year, ct);
        var site = await _siteService.GetByIdAsync(siteId, ct);
        ViewBag.SiteId = siteId;
        ViewBag.SiteName = site?.Name ?? "";
        ViewBag.Year = year;
        return View(report);
    }
}
