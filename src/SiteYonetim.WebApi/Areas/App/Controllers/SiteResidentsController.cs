using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;

namespace SiteYonetim.WebApi.Areas.App.Controllers;

[Area("App")]
[Authorize]
public class SiteResidentsController : Controller
{
    private readonly IApartmentService _apartmentService;
    private readonly ISiteService _siteService;

    public SiteResidentsController(IApartmentService apartmentService, ISiteService siteService)
    {
        _apartmentService = apartmentService;
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
            ViewBag.PageTitle = "Site Sakinleri - Site Seçin";
            return View("SelectSite");
        }
        var apartments = await _apartmentService.GetBySiteIdAsync(siteId.Value, ct);
        var site = await _siteService.GetByIdAsync(siteId.Value, ct);
        ViewBag.SiteId = siteId;
        ViewBag.SiteName = site?.Name ?? "";
        return View(apartments);
    }
}
