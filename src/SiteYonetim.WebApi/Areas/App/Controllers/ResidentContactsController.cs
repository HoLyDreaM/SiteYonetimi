using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;

namespace SiteYonetim.WebApi.Areas.App.Controllers;

[Area("App")]
[Authorize]
public class ResidentContactsController : Controller
{
    private readonly IResidentContactService _contactService;
    private readonly ISiteService _siteService;
    private readonly IApartmentService _apartmentService;

    public ResidentContactsController(IResidentContactService contactService, ISiteService siteService, IApartmentService apartmentService)
    {
        _contactService = contactService;
        _siteService = siteService;
        _apartmentService = apartmentService;
    }

    public async Task<IActionResult> Index(Guid? siteId, CancellationToken ct = default)
    {
        if (!siteId.HasValue)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return RedirectToAction("Login", "Account", new { area = "" });
            ViewBag.Sites = await _siteService.GetUserSitesAsync(userId, ct);
            ViewBag.PageTitle = "Kat Malikleri ve Kiracılar - Site Seçin";
            return View("SelectSite");
        }
        var list = await _contactService.GetBySiteIdAsync(siteId.Value, ct);
        ViewBag.SiteId = siteId;
        ViewBag.Apartments = await _apartmentService.GetBySiteIdAsync(siteId.Value, ct);
        return View(list);
    }

    public async Task<IActionResult> Create(Guid siteId, CancellationToken ct = default)
    {
        var site = await _siteService.GetByIdAsync(siteId, ct);
        if (site == null) return NotFound();
        ViewBag.SiteId = siteId;
        ViewBag.Apartments = await _apartmentService.GetBySiteIdAsync(siteId, ct);
        return View(new ResidentContact { SiteId = siteId, ContactType = ResidentContactType.Owner, IsDeleted = false });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ResidentContact model, CancellationToken ct = default)
    {
        if (model.SiteId == Guid.Empty)
        {
            ModelState.AddModelError("", "Site seçimi gerekli.");
            ViewBag.SiteId = Guid.Empty;
            return View(model);
        }
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError("Name", "Ad Soyad gerekli.");
            ViewBag.SiteId = model.SiteId;
            ViewBag.Apartments = await _apartmentService.GetBySiteIdAsync(model.SiteId, ct);
            return View(model);
        }
        if (string.IsNullOrWhiteSpace(model.Phone))
        {
            ModelState.AddModelError("Phone", "Telefon numarası gerekli.");
            ViewBag.SiteId = model.SiteId;
            ViewBag.Apartments = await _apartmentService.GetBySiteIdAsync(model.SiteId, ct);
            return View(model);
        }
        model.IsDeleted = false;
        await _contactService.CreateAsync(model, ct);
        return RedirectToAction(nameof(Index), new { area = "App", siteId = model.SiteId });
    }

    public async Task<IActionResult> Edit(Guid id, Guid? siteId, CancellationToken ct = default)
    {
        var c = await _contactService.GetByIdAsync(id, ct);
        if (c == null) return NotFound();
        ViewBag.SiteId = c.SiteId;
        ViewBag.Apartments = await _apartmentService.GetBySiteIdAsync(c.SiteId, ct);
        return View(c);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ResidentContact model, CancellationToken ct = default)
    {
        if (id != model.Id) return BadRequest();
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError("Name", "Ad Soyad gerekli.");
            ViewBag.SiteId = model.SiteId;
            ViewBag.Apartments = await _apartmentService.GetBySiteIdAsync(model.SiteId, ct);
            return View(model);
        }
        if (string.IsNullOrWhiteSpace(model.Phone))
        {
            ModelState.AddModelError("Phone", "Telefon numarası gerekli.");
            ViewBag.SiteId = model.SiteId;
            ViewBag.Apartments = await _apartmentService.GetBySiteIdAsync(model.SiteId, ct);
            return View(model);
        }
        await _contactService.UpdateAsync(model, ct);
        return RedirectToAction(nameof(Index), new { area = "App", siteId = model.SiteId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] Guid siteId, CancellationToken ct = default)
    {
        await _contactService.DeleteAsync(id, ct);
        return RedirectToAction(nameof(Index), new { area = "App", siteId });
    }
}
