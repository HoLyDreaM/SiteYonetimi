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

    public async Task<IActionResult> Index(Guid? siteId, DateTime? from, DateTime? to, CancellationToken ct)
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
        var list = await _paymentService.GetBySiteIdAsync(siteId.Value, from, to, null, ct);
        ViewBag.SiteId = siteId;
        ViewBag.From = from ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        ViewBag.To = to ?? DateTime.Today;
        return View(list);
    }
}
