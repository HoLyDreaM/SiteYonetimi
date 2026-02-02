using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;

namespace SiteYonetim.WebApi.Areas.App.Controllers;

[Area("App")]
[Authorize]
public class QuotationsController : Controller
{
    private readonly IQuotationService _quotationService;
    private readonly ISiteService _siteService;
    private readonly IWebHostEnvironment _env;

    public QuotationsController(IQuotationService quotationService, ISiteService siteService, IWebHostEnvironment env)
    {
        _quotationService = quotationService;
        _siteService = siteService;
        _env = env;
    }

    public async Task<IActionResult> Index(Guid? siteId, CancellationToken ct = default)
    {
        if (!siteId.HasValue)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return RedirectToAction("Login", "Account", new { area = "" });
            ViewBag.Sites = await _siteService.GetUserSitesAsync(userId, ct);
            ViewBag.PageTitle = "Teklifler - Site Seçin";
            return View("SelectSite");
        }
        var list = await _quotationService.GetBySiteIdAsync(siteId.Value, ct);
        ViewBag.SiteId = siteId;
        return View(list);
    }

    public async Task<IActionResult> Create(Guid siteId, CancellationToken ct = default)
    {
        var site = await _siteService.GetByIdAsync(siteId, ct);
        if (site == null) return NotFound();
        ViewBag.SiteId = siteId;
        return View(new Quotation { SiteId = siteId, QuotationDate = DateTime.Today, IsDeleted = false });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Quotation model, IFormFile? Dosya, CancellationToken ct = default)
    {
        if (model.SiteId == Guid.Empty)
        {
            ModelState.AddModelError("", "Site seçimi gerekli.");
            ViewBag.SiteId = Guid.Empty;
            return View(model);
        }
        var site = await _siteService.GetByIdAsync(model.SiteId, ct);
        if (site == null) return NotFound();
        if (string.IsNullOrWhiteSpace(model.CompanyName))
        {
            ModelState.AddModelError("CompanyName", "Firma adı gerekli.");
            ViewBag.SiteId = model.SiteId;
            return View(model);
        }
        if (Dosya != null && Dosya.Length > 0)
        {
            var uploadsDir = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads", "teklifler");
            Directory.CreateDirectory(uploadsDir);
            var ext = Path.GetExtension(Dosya.FileName) ?? ".pdf";
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);
            await using (var stream = new FileStream(filePath, FileMode.Create))
                await Dosya.CopyToAsync(stream);
            model.FilePath = $"/uploads/teklifler/{fileName}";
        }
        model.IsDeleted = false;
        await _quotationService.CreateAsync(model, ct);
        return RedirectToAction(nameof(Index), new { area = "App", siteId = model.SiteId });
    }

    public async Task<IActionResult> Edit(Guid id, Guid? siteId, CancellationToken ct = default)
    {
        var q = await _quotationService.GetByIdAsync(id, ct);
        if (q == null) return NotFound();
        ViewBag.SiteId = q.SiteId;
        return View(q);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Quotation model, IFormFile? Dosya, CancellationToken ct = default)
    {
        if (id != model.Id) return BadRequest();
        if (string.IsNullOrWhiteSpace(model.CompanyName))
        {
            ModelState.AddModelError("CompanyName", "Firma adı gerekli.");
            ViewBag.SiteId = model.SiteId;
            return View(model);
        }
        var existing = await _quotationService.GetByIdAsync(id, ct);
        if (existing != null && Dosya != null && Dosya.Length > 0)
        {
            var uploadsDir = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads", "teklifler");
            Directory.CreateDirectory(uploadsDir);
            var ext = Path.GetExtension(Dosya.FileName) ?? ".pdf";
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);
            await using (var stream = new FileStream(filePath, FileMode.Create))
                await Dosya.CopyToAsync(stream);
            model.FilePath = $"/uploads/teklifler/{fileName}";
        }
        else if (existing != null && string.IsNullOrEmpty(model.FilePath))
        {
            model.FilePath = existing.FilePath;
        }
        await _quotationService.UpdateAsync(model, ct);
        return RedirectToAction(nameof(Index), new { area = "App", siteId = model.SiteId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] Guid siteId, CancellationToken ct = default)
    {
        await _quotationService.DeleteAsync(id, ct);
        return RedirectToAction(nameof(Index), new { area = "App", siteId });
    }
}
