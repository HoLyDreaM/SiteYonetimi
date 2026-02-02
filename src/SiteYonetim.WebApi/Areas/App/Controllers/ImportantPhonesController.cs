using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;

namespace SiteYonetim.WebApi.Areas.App.Controllers;

[Area("App")]
[Authorize]
public class ImportantPhonesController : Controller
{
    private readonly IImportantPhoneService _phoneService;
    private readonly ISiteService _siteService;

    public ImportantPhonesController(IImportantPhoneService phoneService, ISiteService siteService)
    {
        _phoneService = phoneService;
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
            ViewBag.PageTitle = "Önemli Telefonlar - Site Seçin";
            return View("SelectSite");
        }
        var list = await _phoneService.GetBySiteIdAsync(siteId.Value, ct);
        ViewBag.SiteId = siteId;
        return View(list);
    }

    public async Task<IActionResult> Create(Guid siteId, CancellationToken ct = default)
    {
        var site = await _siteService.GetByIdAsync(siteId, ct);
        if (site == null) return NotFound();
        ViewBag.SiteId = siteId;
        return View(new ImportantPhone { SiteId = siteId, IsDeleted = false });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ImportantPhone model, CancellationToken ct = default)
    {
        if (model.SiteId == Guid.Empty)
        {
            ModelState.AddModelError("", "Site seçimi gerekli.");
            ViewBag.SiteId = Guid.Empty;
            return View(model);
        }
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError("Name", "İsim gerekli.");
            ViewBag.SiteId = model.SiteId;
            return View(model);
        }
        if (string.IsNullOrWhiteSpace(model.Phone))
        {
            ModelState.AddModelError("Phone", "Telefon numarası gerekli.");
            ViewBag.SiteId = model.SiteId;
            return View(model);
        }
        model.IsDeleted = false;
        await _phoneService.CreateAsync(model, ct);
        return RedirectToAction(nameof(Index), new { area = "App", siteId = model.SiteId });
    }

    public async Task<IActionResult> Edit(Guid id, Guid? siteId, CancellationToken ct = default)
    {
        var p = await _phoneService.GetByIdAsync(id, ct);
        if (p == null) return NotFound();
        ViewBag.SiteId = p.SiteId;
        return View(p);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ImportantPhone model, CancellationToken ct = default)
    {
        if (id != model.Id) return BadRequest();
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError("Name", "İsim gerekli.");
            ViewBag.SiteId = model.SiteId;
            return View(model);
        }
        if (string.IsNullOrWhiteSpace(model.Phone))
        {
            ModelState.AddModelError("Phone", "Telefon numarası gerekli.");
            ViewBag.SiteId = model.SiteId;
            return View(model);
        }
        await _phoneService.UpdateAsync(model, ct);
        return RedirectToAction(nameof(Index), new { area = "App", siteId = model.SiteId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] Guid siteId, CancellationToken ct = default)
    {
        await _phoneService.DeleteAsync(id, ct);
        return RedirectToAction(nameof(Index), new { area = "App", siteId });
    }
}
