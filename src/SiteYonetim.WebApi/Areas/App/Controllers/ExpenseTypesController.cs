using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;

namespace SiteYonetim.WebApi.Areas.App.Controllers;

[Area("App")]
[Authorize]
public class ExpenseTypesController : Controller
{
    private readonly IExpenseTypeService _expenseTypeService;
    private readonly ISiteService _siteService;

    public ExpenseTypesController(IExpenseTypeService expenseTypeService, ISiteService siteService)
    {
        _expenseTypeService = expenseTypeService;
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
            ViewBag.PageTitle = "Gider Türleri - Site Seçin";
            return View("SelectSite");
        }
        var list = await _expenseTypeService.GetBySiteIdAsync(siteId.Value, ct);
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
            ViewBag.PageTitle = "Gider Türleri - Site Seçin";
            return View("SelectSite");
        }
        var site = await _siteService.GetByIdAsync(siteId, ct);
        if (site == null) return NotFound();
        ViewBag.SiteId = siteId;
        return View(new ExpenseType { SiteId = siteId, IsDeleted = false });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ExpenseType model, CancellationToken ct)
    {
        if (model.SiteId == Guid.Empty)
        {
            ModelState.AddModelError("", "Site seçimi gerekli. Lütfen Gider Türleri sayfasından bir site seçip tekrar deneyin.");
            ViewBag.SiteId = Guid.Empty;
            return View(model);
        }
        var site = await _siteService.GetByIdAsync(model.SiteId, ct);
        if (site == null)
        {
            ModelState.AddModelError("", "Seçilen site bulunamadı.");
            ViewBag.SiteId = model.SiteId;
            return View(model);
        }
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError("Name", "Gider türü adı gerekli.");
            ViewBag.SiteId = model.SiteId;
            return View(model);
        }
        try
        {
            model.IsDeleted = false;
            await _expenseTypeService.CreateAsync(model, ct);
            return RedirectToAction(nameof(Index), new { area = "App", siteId = model.SiteId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Kayıt sırasında hata: {ex.Message}");
            ViewBag.SiteId = model.SiteId;
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(Guid id, Guid? siteId, CancellationToken ct = default)
    {
        var et = await _expenseTypeService.GetByIdAsync(id, ct);
        if (et == null) return NotFound();
        ViewBag.SiteId = et.SiteId;
        return View(et);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ExpenseType model, CancellationToken ct)
    {
        if (id != model.Id) return BadRequest();
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError("Name", "Gider türü adı gerekli.");
            ViewBag.SiteId = model.SiteId;
            return View(model);
        }
        try
        {
            await _expenseTypeService.UpdateAsync(model, ct);
            return RedirectToAction(nameof(Index), new { area = "App", siteId = model.SiteId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Güncelleme sırasında hata: {ex.Message}");
            ViewBag.SiteId = model.SiteId;
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, Guid siteId, CancellationToken ct)
    {
        await _expenseTypeService.DeleteAsync(id, ct);
        return RedirectToAction(nameof(Index), new { area = "App", siteId });
    }
}
