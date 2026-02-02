using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteYonetim.Domain.Interfaces;

namespace SiteYonetim.WebApi.Areas.App.Controllers;

[Area("App")]
[Authorize]
public class DashboardController : Controller
{
    private readonly ISiteService _siteService;

    public DashboardController(ISiteService siteService) => _siteService = siteService;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return RedirectToAction("Login", "Account", new { area = "" });
        var sites = await _siteService.GetUserSitesAsync(userId, ct);
        return View(sites);
    }
}
