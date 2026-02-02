using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;

namespace SiteYonetim.WebApi.Areas.App.Controllers;

[Area("App")]
[Authorize]
public class MetersController : Controller
{
    private readonly IMeterService _meterService;
    private readonly ISiteService _siteService;

    public MetersController(IMeterService meterService, ISiteService siteService)
    {
        _meterService = meterService;
        _siteService = siteService;
    }

    public async Task<IActionResult> Index(Guid? siteId, CancellationToken ct)
    {
        if (!siteId.HasValue)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return RedirectToAction("Login", "Account", new { area = "" });
            var sites = await _siteService.GetUserSitesAsync(userId, ct);
            ViewBag.Sites = sites;
            ViewBag.PageTitle = "Sayaçlar - Site Seçin";
            return View("SelectSite");
        }
        var list = await _meterService.GetBySiteIdAsync(siteId.Value, null, ct);
        ViewBag.SiteId = siteId;
        return View(list);
    }

    public async Task<IActionResult> Create(Guid siteId, CancellationToken ct = default)
    {
        if (siteId == Guid.Empty)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return RedirectToAction("Login", "Account", new { area = "" });
            var sites = await _siteService.GetUserSitesAsync(userId, ct);
            ViewBag.Sites = sites;
            ViewBag.PageTitle = "Sayaçlar - Site Seçin";
            return View("SelectSite");
        }
        var site = await _siteService.GetByIdAsync(siteId, ct);
        if (site == null) return NotFound();
        ViewBag.SiteId = siteId;
        return View(new Meter { SiteId = siteId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Meter model, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError("Name", "Sayaç adı gerekli.");
            ViewBag.SiteId = model.SiteId;
            return View(model);
        }
        await _meterService.CreateAsync(model, ct);
        return RedirectToAction(nameof(Index), new { area = "App", siteId = model.SiteId });
    }
}
