using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteYonetim.Domain.Interfaces;

namespace SiteYonetim.WebApi.Areas.App.Controllers;

[Area("App")]
[Authorize]
public class DebtorsController : Controller
{
    private readonly IReportService _reportService;
    private readonly ISiteService _siteService;

    public DebtorsController(IReportService reportService, ISiteService siteService)
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
            ViewBag.PageTitle = "Borçlular - Site Seçin";
            return View("SelectSite");
        }
        var list = await _reportService.GetDebtorsAsync(siteId.Value, ct);
        ViewBag.SiteId = siteId;
        ViewBag.TotalDebt = list.Sum(x => x.TotalDebt);
        return View(list);
    }
}
