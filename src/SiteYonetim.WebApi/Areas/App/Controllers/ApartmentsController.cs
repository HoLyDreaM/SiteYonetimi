using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;

namespace SiteYonetim.WebApi.Areas.App.Controllers;

[Area("App")]
[Authorize]
public class ApartmentsController : Controller
{
    private readonly IApartmentService _apartmentService;
    private readonly ISiteService _siteService;

    public ApartmentsController(IApartmentService apartmentService, ISiteService siteService)
    {
        _apartmentService = apartmentService;
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
            ViewBag.PageTitle = "Daireler - Site Seçin";
            return View("SelectSite");
        }
        var list = await _apartmentService.GetBySiteIdAsync(siteId.Value, ct);
        ViewBag.SiteId = siteId;
        return View(list);
    }

    public async Task<IActionResult> Create(Guid siteId, CancellationToken ct = default)
    {
        var site = await _siteService.GetByIdAsync(siteId, ct);
        if (site == null) return NotFound();
        ViewBag.SiteId = siteId;
        ViewBag.SiteName = site.Name;
        return View(new Apartment { SiteId = siteId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Apartment model, CancellationToken ct)
    {
        if (model.SiteId == Guid.Empty)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return RedirectToAction("Login", "Account", new { area = "" });
            ModelState.AddModelError("", "Site seçimi geçersiz. Lütfen sitelerden bir site seçip tekrar deneyin.");
            ViewBag.PageTitle = "Daireler - Site Seçin";
            ViewBag.Sites = await _siteService.GetUserSitesAsync(userId, ct);
            return View("SelectSite");
        }
        var site = await _siteService.GetByIdAsync(model.SiteId, ct);
        if (site == null)
        {
            ModelState.AddModelError("", "Seçilen site bulunamadı.");
            ViewBag.SiteId = model.SiteId;
            ViewBag.SiteName = "";
            return View(model);
        }
        if (string.IsNullOrWhiteSpace(model.ApartmentNumber))
        {
            ModelState.AddModelError("ApartmentNumber", "Daire no gerekli.");
            ViewBag.SiteId = model.SiteId;
            ViewBag.SiteName = site.Name;
            return View(model);
        }
        if (model.OccupancyType == ApartmentOccupancyType.TenantOccupied)
        {
            if (string.IsNullOrWhiteSpace(model.OwnerName))
                ModelState.AddModelError("OwnerName", "Kiracı oturuyorsa ev sahibi adı zorunludur.");
            if (string.IsNullOrWhiteSpace(model.TenantName))
                ModelState.AddModelError("TenantName", "Kiracı adı zorunludur.");
            if (!ModelState.IsValid)
            {
                ViewBag.SiteId = model.SiteId;
                ViewBag.SiteName = site.Name;
                return View(model);
            }
        }
        model.IsDeleted = false;
        await _apartmentService.CreateAsync(model, ct);
        return RedirectToAction(nameof(Index), new { area = "App", siteId = model.SiteId });
    }

    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var apt = await _apartmentService.GetByIdAsync(id, ct);
        if (apt == null) return NotFound();
        return View(apt);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Apartment model, CancellationToken ct)
    {
        if (id != model.Id) return BadRequest();
        await _apartmentService.UpdateAsync(model, ct);
        return RedirectToAction(nameof(Index), new { area = "App", siteId = model.SiteId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] Guid siteId, CancellationToken ct = default)
    {
        await _apartmentService.DeleteAsync(id, ct);
        return RedirectToAction(nameof(Index), new { area = "App", siteId });
    }
}
