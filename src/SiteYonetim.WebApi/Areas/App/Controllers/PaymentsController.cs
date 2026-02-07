using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;

namespace SiteYonetim.WebApi.Areas.App.Controllers;

[Area("App")]
[Authorize]
public class PaymentsController : Controller
{
    private readonly IPaymentService _paymentService;
    private readonly ISiteService _siteService;

    public PaymentsController(IPaymentService paymentService, ISiteService siteService)
    {
        _paymentService = paymentService;
        _siteService = siteService;
    }

    public async Task<IActionResult> Index(Guid? siteId, int? year, int? month, CancellationToken ct)
    {
        if (!siteId.HasValue)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return RedirectToAction("Login", "Account", new { area = "" });
            var sites = await _siteService.GetUserSitesAsync(userId, ct);
            ViewBag.Sites = sites;
            ViewBag.PageTitle = "Tahsilatlar - Site Seçin";
            return View("SelectSite");
        }
        var y = year ?? DateTime.Today.Year;
        var m = month ?? DateTime.Today.Month;
        var from = new DateTime(y, m, 1);
        var to = new DateTime(y, m, DateTime.DaysInMonth(y, m));
        var list = await _paymentService.GetBySiteIdAsync(siteId.Value, from, to, null, ct);
        ViewBag.SiteId = siteId;
        ViewBag.Year = y;
        ViewBag.Month = m;
        ViewBag.SiteName = (await _siteService.GetByIdAsync(siteId.Value, ct))?.Name ?? "";
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, Guid siteId, int year, int month, CancellationToken ct = default)
    {
        var ok = await _paymentService.DeleteAsync(id, ct);
        if (ok)
            TempData["Message"] = "Tahsilat iptal edildi. Aidat tekrar tahsil edilebilir.";
        else
            TempData["Error"] = "Tahsilat iptal edilemedi.";
        return RedirectToAction(nameof(Index), new { area = "App", siteId, year, month });
    }
}
