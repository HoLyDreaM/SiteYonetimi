using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteYonetim.Domain.Interfaces;

namespace SiteYonetim.WebApi.Areas.App.Controllers;

[Area("App")]
[Authorize]
public class SupportSettingsController : Controller
{
    private readonly ISiteService _siteService;

    public SupportSettingsController(ISiteService siteService)
    {
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
            ViewBag.PageTitle = "Destek Kayıt Ayarları - Site Seçin";
            return View("SelectSite");
        }
        var site = await _siteService.GetByIdAsync(siteId.Value, ct);
        if (site == null) return NotFound();
        ViewBag.SiteId = siteId;
        ViewBag.SiteName = site.Name;
        return View(site);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SupportSettingsModel model, CancellationToken ct = default)
    {
        var site = await _siteService.GetByIdAsync(model.SiteId, ct);
        if (site == null) return NotFound();
        site.SupportNotificationEmail = string.IsNullOrWhiteSpace(model.SupportNotificationEmail) ? null : model.SupportNotificationEmail.Trim();
        site.SupportSmtpHost = string.IsNullOrWhiteSpace(model.SupportSmtpHost) ? null : model.SupportSmtpHost.Trim();
        site.SupportSmtpPort = model.SupportSmtpPort;
        site.SupportSmtpUsername = string.IsNullOrWhiteSpace(model.SupportSmtpUsername) ? null : model.SupportSmtpUsername.Trim();
        if (!string.IsNullOrWhiteSpace(model.SupportSmtpPassword))
            site.SupportSmtpPassword = model.SupportSmtpPassword;
        await _siteService.UpdateAsync(site, ct);
        ViewBag.SiteId = model.SiteId;
        ViewBag.SiteName = site.Name;
        ViewBag.Success = true;
        return View(site);
    }
}

public class SupportSettingsModel
{
    public Guid SiteId { get; set; }
    public string? SupportNotificationEmail { get; set; }
    public string? SupportSmtpHost { get; set; }
    public int? SupportSmtpPort { get; set; }
    public string? SupportSmtpUsername { get; set; }
    public string? SupportSmtpPassword { get; set; }
}
