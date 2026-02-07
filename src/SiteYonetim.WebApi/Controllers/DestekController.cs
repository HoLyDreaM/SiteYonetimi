using Microsoft.AspNetCore.Mvc;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;

namespace SiteYonetim.WebApi.Controllers;

/// <summary>
/// Destek kaydı formu - Üyelik gerekmez. URL'de siteId ile herkes destek oluşturabilir.
/// URL: /Destek?siteId=xxx veya /DestekKaydi?siteId=xxx
/// Alanlar: İsim soyisim, E-posta, Blok No, Kat No, Konu başlığı, İstek/Öneri, Açıklama, Resim
/// </summary>
public class DestekController : Controller
{
    private readonly ISiteService _siteService;
    private readonly ISupportTicketService _ticketService;
    private readonly IWebHostEnvironment _env;
    private readonly IEmailService _emailService;

    public DestekController(ISiteService siteService, ISupportTicketService ticketService, IWebHostEnvironment env, IEmailService emailService)
    {
        _siteService = siteService;
        _ticketService = ticketService;
        _env = env;
        _emailService = emailService;
    }

    [HttpGet]
    [Route("Destek")]
    [Route("DestekKaydi")]
    public async Task<IActionResult> Create(Guid? siteId, CancellationToken ct = default)
    {
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
        var name = (User.Identity?.IsAuthenticated ?? false) ? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "" : "";
        var email = (User.Identity?.IsAuthenticated ?? false) ? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "" : "";
        ViewBag.IsAuthenticated = User.Identity?.IsAuthenticated ?? false;
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
            ContactEmail = model.ContactEmail,
            ContactPhone = model.ContactPhone,
            BlockNumber = model.BlockNumber,
            FloorNumber = model.FloorNumber,
            ApartmentNumber = model.ApartmentNumber,
            TopicType = model.TopicType switch { "Complaint" => TicketTopicType.Complaint, "Request" => TicketTopicType.Request, _ => TicketTopicType.Suggestion },
            Subject = model.Subject!,
            Message = model.Message!,
            Status = TicketStatus.Open,
            Priority = TicketPriority.Medium,
            IsDeleted = false
        };
        await _ticketService.CreateAsync(ticket, ct);

        if (!string.IsNullOrWhiteSpace(site.SupportNotificationEmail))
        {
            var body = $@"
<h3>Yeni Destek Kaydı</h3>
<p><strong>Site:</strong> {site.Name}</p>
<p><strong>Konu:</strong> {ticket.Subject}</p>
<p><strong>Gönderen:</strong> {ticket.ContactName} ({ticket.ContactEmail ?? ticket.ContactPhone ?? "-"})</p>
<p><strong>Blok/Kat/Daire:</strong> {ticket.BlockNumber ?? "-"} / {ticket.FloorNumber?.ToString() ?? "-"} / {ticket.ApartmentNumber ?? "-"}</p>
<p><strong>Mesaj:</strong></p>
<p>{ticket.Message}</p>
<p><em>Kayıt no: {ticket.Id}</em></p>";
            await _emailService.SendWithSiteSmtpAsync(site.SupportNotificationEmail, $"[Destek] {ticket.Subject}", body,
                site.SupportSmtpHost, site.SupportSmtpPort, site.SupportSmtpUsername, site.SupportSmtpPassword, ct);
        }

        if (Resim != null && Resim.Length > 0)
        {
            var uploadsDir = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads", "destek", model.SiteId.ToString("N"));
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
                FilePath = $"/uploads/destek/{model.SiteId:N}/{fileName}",
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
    public string? ApartmentNumber { get; set; }
    public string? Subject { get; set; }
    public string TopicType { get; set; } = "Suggestion"; // Suggestion, Complaint, Request
    public string? Message { get; set; }
}
