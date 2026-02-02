using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;

namespace SiteYonetim.WebApi.Controllers;

/// <summary>
/// Öneri / Şikayet formu - Giriş gerekmez. URL: /Feedback/Create?siteId=xxx veya /OneriSikayet?siteId=xxx
/// </summary>
[AllowAnonymous]
public class FeedbackController : Controller
{
    private readonly ISiteService _siteService;
    private readonly ISupportTicketService _ticketService;
    private readonly IApartmentService _apartmentService;

    public FeedbackController(ISiteService siteService, ISupportTicketService ticketService, IApartmentService apartmentService)
    {
        _siteService = siteService;
        _ticketService = ticketService;
        _apartmentService = apartmentService;
    }

    [HttpGet]
    [Route("Feedback/Create")]
    [Route("OneriSikayet")]
    public async Task<IActionResult> Create(Guid? siteId, CancellationToken ct = default)
    {
        if (!siteId.HasValue)
        {
            ViewBag.Message = "Bu sayfaya site yönetiminden alacağınız link ile ulaşın. (Örn: .../OneriSikayet?siteId=...)";
            return View("NoSite");
        }
        var site = await _siteService.GetByIdAsync(siteId.Value, ct);
        if (site == null)
        {
            ViewBag.Message = "Geçersiz site.";
            return View("NoSite");
        }
        ViewBag.SiteId = siteId;
        ViewBag.SiteName = site.Name;
        return View(new FeedbackInputModel { SiteId = siteId.Value });
    }

    [HttpPost]
    [Route("Feedback/Create")]
    [Route("OneriSikayet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FeedbackInputModel model, CancellationToken ct = default)
    {
        var site = await _siteService.GetByIdAsync(model.SiteId, ct);
        if (site == null) { ModelState.AddModelError("", "Geçersiz site."); return View(model); }
        ViewBag.SiteId = model.SiteId;
        ViewBag.SiteName = site.Name;

        if (string.IsNullOrWhiteSpace(model.ContactName))
            ModelState.AddModelError("ContactName", "İsim soyisim gerekli.");
        if (string.IsNullOrWhiteSpace(model.Subject))
            ModelState.AddModelError("Subject", "Konu gerekli.");
        if (string.IsNullOrWhiteSpace(model.Message))
            ModelState.AddModelError("Message", "Açıklama gerekli.");

        if (!ModelState.IsValid)
            return View(model);

        Guid? apartmentId = null;
        if (!string.IsNullOrWhiteSpace(model.BlockOrBuildingName) || !string.IsNullOrWhiteSpace(model.ApartmentNumber))
        {
            var apartments = await _apartmentService.GetBySiteIdAsync(model.SiteId, ct);
            var match = apartments.FirstOrDefault(a =>
                a.BlockOrBuildingName.Equals(model.BlockOrBuildingName ?? "", StringComparison.OrdinalIgnoreCase) &&
                a.ApartmentNumber.Equals(model.ApartmentNumber ?? "", StringComparison.OrdinalIgnoreCase));
            if (match != null)
                apartmentId = match.Id;
        }

        var ticket = new SupportTicket
        {
            SiteId = model.SiteId,
            ApartmentId = apartmentId,
            ContactName = model.ContactName,
            ContactPhone = model.ContactPhone,
            TopicType = model.TopicType == "Complaint" ? TicketTopicType.Complaint : TicketTopicType.Suggestion,
            Subject = model.Subject!,
            Message = model.Message!,
            Status = TicketStatus.Open,
            Priority = TicketPriority.Medium,
            IsDeleted = false
        };
        await _ticketService.CreateAsync(ticket, ct);
        ViewBag.Success = true;
        return View("Create", model);
    }
}

public class FeedbackInputModel
{
    public Guid SiteId { get; set; }
    public string? BlockOrBuildingName { get; set; }
    public string? ApartmentNumber { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string? Subject { get; set; }
    /// <summary>Öneri veya Şikayet</summary>
    public string TopicType { get; set; } = "Suggestion";
    public string? Message { get; set; }
}
