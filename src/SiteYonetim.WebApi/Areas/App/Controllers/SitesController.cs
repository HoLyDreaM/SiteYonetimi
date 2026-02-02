using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;

namespace SiteYonetim.WebApi.Areas.App.Controllers;

[Area("App")]
[Authorize]
public class SitesController : Controller
{
    private readonly ISiteService _siteService;

    public SitesController(ISiteService siteService) => _siteService = siteService;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return RedirectToAction("Login", "Account", new { area = "" });
        var list = await _siteService.GetUserSitesAsync(userId, ct);
        return View(list);
    }

    public IActionResult Create() => View(new Site());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Site model, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError("Name", "Site adı gerekli.");
            return View(model);
        }
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        Guid? userId = Guid.TryParse(userIdClaim, out var u) ? u : null;
        await _siteService.CreateAsync(model, userId, ct);
        return RedirectToAction(nameof(Index), new { area = "App" });
    }

    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var site = await _siteService.GetByIdAsync(id, ct);
        if (site == null) return NotFound();
        return View(site);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Site model, CancellationToken ct)
    {
        if (id != model.Id) return BadRequest();
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError("Name", "Site adı gerekli.");
            return View(model);
        }
        await _siteService.UpdateAsync(model, ct);
        return RedirectToAction(nameof(Index), new { area = "App" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _siteService.DeleteAsync(id, ct);
        return RedirectToAction(nameof(Index), new { area = "App" });
    }
}
