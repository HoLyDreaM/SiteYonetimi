using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;

namespace SiteYonetim.WebApi.Controllers;

/// <summary>
/// Destek kaydı formu - Üyelik (giriş) gerekir. URL: /Destek?siteId=xxx veya /DestekKaydi?siteId=xxx
/// Alanlar: İsim soyisim (otomatik), Blok No, Kat No, Konu başlığı, İstek/Öneri, Açıklama, Resim
/// </summary>
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public class DestekController : Controller
{
    private readonly ISiteService _siteService;
    private readonly ISupportTicketService _ticketService;
    private readonly IWebHostEnvironment _env;

    public DestekController(ISiteService siteService, ISupportTicketService ticketService, IWebHostEnvironment env)
    {
        _siteService = siteService;
        _ticketService = ticketService;
        _env = env;
    }

    [HttpGet]
    [Route("Destek")]
    [Route("DestekKaydi")]
    public async Task<IActionResult> Create(Guid? siteId, CancellationToken ct = default)
    {
        if (!(User.Identity?.IsAuthenticated ?? false))
            return RedirectToAction("Login", "Account", new { returnUrl = Request.Path + Request.QueryString });
        if (!siteId.HasValue)
        {
            ViewBag.Message = "Bu sayfaya site yönetiminden alacağınız link ile ulaşın. (Örn: .../Destek?siteId=...)";
            return View("NoSite");
        }
        var site = await _siteService.GetByIdAsync(siteId.Value, ct);
        if (site == null)
        {
            ViewBag.Message = "Geçersiz site.";
            return View("NoSite");
        }
        var name = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "";
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "";
        ViewBag.SiteId = siteId;
        ViewBag.SiteName = site.Name;
        return View(new DestekInputModel { SiteId = siteId.Value, ContactName = name, ContactEmail = email });
    }

    [HttpPost]
    [Route("Destek")]
    [Route("DestekKaydi")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DestekInputModel model, IFormFile? Resim, CancellationToken ct = default)
    {
        if (!(User.Identity?.IsAuthenticated ?? false))
            return RedirectToAction("Login", "Account", new { returnUrl = $"/Destek?siteId={model.SiteId}" });
        var site = await _siteService.GetByIdAsync(model.SiteId, ct);
        if (site == null) { ModelState.AddModelError("", "Geçersiz site."); return View(model); }
        ViewBag.SiteId = model.SiteId;
        ViewBag.SiteName = site.Name;

        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        Guid? createdByUserId = null;
        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var uid))
            createdByUserId = uid;
        if (string.IsNullOrWhiteSpace(model.ContactName))
            model.ContactName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "";
        if (string.IsNullOrWhiteSpace(model.ContactName))
            ModelState.AddModelError("ContactName", "İsim soyisim gerekli.");
        if (string.IsNullOrWhiteSpace(model.Subject))
            ModelState.AddModelError("Subject", "Konu başlığı gerekli.");
        if (string.IsNullOrWhiteSpace(model.Message))
            ModelState.AddModelError("Message", "Açıklama gerekli.");

        if (!ModelState.IsValid)
            return View(model);

        var ticket = new SupportTicket
        {
            SiteId = model.SiteId,
            CreatedByUserId = createdByUserId,
            ContactName = model.ContactName,
            ContactPhone = model.ContactPhone,
            BlockNumber = model.BlockNumber,
            FloorNumber = model.FloorNumber,
            TopicType = model.TopicType switch { "Complaint" => TicketTopicType.Complaint, "Request" => TicketTopicType.Request, _ => TicketTopicType.Suggestion },
            Subject = model.Subject!,
            Message = model.Message!,
            Status = TicketStatus.Open,
            Priority = TicketPriority.Medium,
            IsDeleted = false
        };
        await _ticketService.CreateAsync(ticket, ct);

        if (Resim != null && Resim.Length > 0)
        {
            var uploadsDir = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads", "destek");
            Directory.CreateDirectory(uploadsDir);
            var ext = Path.GetExtension(Resim.FileName) ?? ".jpg";
            var fileName = $"{ticket.Id}_{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);
            await using (var stream = new FileStream(filePath, FileMode.Create))
                await Resim.CopyToAsync(stream);
            await _ticketService.AddAttachmentAsync(new SupportTicketAttachment
            {
                SupportTicketId = ticket.Id,
                FileName = Resim.FileName,
                FilePath = $"/uploads/destek/{fileName}",
                IsDeleted = false
            }, ct);
        }

        ViewBag.Success = true;
        return View("Create", model);
    }
}

public class DestekInputModel
{
    public Guid SiteId { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? BlockNumber { get; set; }
    public int? FloorNumber { get; set; }
    public string? Subject { get; set; }
    public string TopicType { get; set; } = "Suggestion"; // Suggestion, Complaint, Request
    public string? Message { get; set; }
}
