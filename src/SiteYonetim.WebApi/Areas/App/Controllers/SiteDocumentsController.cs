using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;

namespace SiteYonetim.WebApi.Areas.App.Controllers;

[Area("App")]
[Authorize]
public class SiteDocumentsController : Controller
{
    private readonly ISiteDocumentService _documentService;
    private readonly ISiteService _siteService;
    private readonly IWebHostEnvironment _env;

    private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx" };

    public SiteDocumentsController(ISiteDocumentService documentService, ISiteService siteService, IWebHostEnvironment env)
    {
        _documentService = documentService;
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
            ViewBag.PageTitle = "Evrak Arşivi - Site Seçin";
            return View("SelectSite");
        }
        var list = await _documentService.GetBySiteIdAsync(siteId.Value, ct);
        ViewBag.SiteId = siteId;
        return View(list);
    }

    public async Task<IActionResult> Create(Guid siteId, CancellationToken ct = default)
    {
        var site = await _siteService.GetByIdAsync(siteId, ct);
        if (site == null) return NotFound();
        ViewBag.SiteId = siteId;
        ViewBag.SiteName = site.Name;
        return View(new SiteDocument { SiteId = siteId, IsDeleted = false });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SiteDocument model, IFormFile? Dosya, CancellationToken ct = default)
    {
        if (model.SiteId == Guid.Empty)
        {
            ModelState.AddModelError("", "Site seçimi gerekli.");
            ViewBag.SiteId = Guid.Empty;
            return View(model);
        }
        var site = await _siteService.GetByIdAsync(model.SiteId, ct);
        if (site == null) return NotFound();
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError("Name", "Evrak adı gerekli.");
            ViewBag.SiteId = model.SiteId;
            ViewBag.SiteName = site.Name;
            return View(model);
        }
        if (Dosya == null || Dosya.Length == 0)
        {
            ModelState.AddModelError("", "Dosya yüklenmeli (PDF, Word veya Excel).");
            ViewBag.SiteId = model.SiteId;
            ViewBag.SiteName = site.Name;
            return View(model);
        }
        var ext = (Path.GetExtension(Dosya.FileName) ?? "").ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
        {
            ModelState.AddModelError("", "Sadece PDF, Word (.doc, .docx) ve Excel (.xls, .xlsx) dosyaları kabul edilir.");
            ViewBag.SiteId = model.SiteId;
            ViewBag.SiteName = site.Name;
            return View(model);
        }
        model.IsDeleted = false;
        model.FileName = Dosya.FileName;
        await _documentService.CreateAsync(model, ct);
        var uploadsDir = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads", "evraklar", model.SiteId.ToString("N"));
        Directory.CreateDirectory(uploadsDir);
        var fileName = $"{model.Id:N}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);
        await using (var stream = new FileStream(filePath, FileMode.Create))
            await Dosya.CopyToAsync(stream);
        model.FilePath = $"/uploads/evraklar/{model.SiteId:N}/{fileName}";
        await _documentService.UpdateAsync(model, ct);
        return RedirectToAction(nameof(Index), new { area = "App", siteId = model.SiteId });
    }

    public async Task<IActionResult> Edit(Guid id, Guid? siteId, CancellationToken ct = default)
    {
        var doc = await _documentService.GetByIdAsync(id, ct);
        if (doc == null) return NotFound();
        ViewBag.SiteId = doc.SiteId;
        return View(doc);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, SiteDocument model, IFormFile? Dosya, CancellationToken ct = default)
    {
        if (id != model.Id) return BadRequest();
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError("Name", "Evrak adı gerekli.");
            ViewBag.SiteId = model.SiteId;
            return View(model);
        }
        var existing = await _documentService.GetByIdAsync(id, ct);
        if (existing == null) return NotFound();
        if (Dosya != null && Dosya.Length > 0)
        {
            var ext = (Path.GetExtension(Dosya.FileName) ?? "").ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
            {
                ModelState.AddModelError("", "Sadece PDF, Word (.doc, .docx) ve Excel (.xls, .xlsx) dosyaları kabul edilir.");
                ViewBag.SiteId = model.SiteId;
                return View(model);
            }
            var uploadsDir = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads", "evraklar", model.SiteId.ToString("N"));
            Directory.CreateDirectory(uploadsDir);
            var fileName = $"{model.Id:N}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);
            await using (var stream = new FileStream(filePath, FileMode.Create))
                await Dosya.CopyToAsync(stream);
            model.FilePath = $"/uploads/evraklar/{model.SiteId:N}/{fileName}";
            model.FileName = Dosya.FileName;
        }
        else
        {
            model.FilePath = existing.FilePath;
            model.FileName = existing.FileName;
        }
        await _documentService.UpdateAsync(model, ct);
        return RedirectToAction(nameof(Index), new { area = "App", siteId = model.SiteId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] Guid siteId, CancellationToken ct = default)
    {
        await _documentService.DeleteAsync(id, ct);
        return RedirectToAction(nameof(Index), new { area = "App", siteId });
    }
}
