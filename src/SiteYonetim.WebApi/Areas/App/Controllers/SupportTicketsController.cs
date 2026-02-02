using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;

namespace SiteYonetim.WebApi.Areas.App.Controllers;

[Area("App")]
[Authorize]
public class SupportTicketsController : Controller
{
    private readonly ISupportTicketService _ticketService;
    private readonly ISiteService _siteService;

    public SupportTicketsController(ISupportTicketService ticketService, ISiteService siteService)
    {
        _ticketService = ticketService;
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
            ViewBag.PageTitle = "Destek Kayıtları - Site Seçin";
            return View("SelectSite");
        }
        var list = await _ticketService.GetBySiteIdAsync(siteId.Value, ct);
        ViewBag.SiteId = siteId;
        return View(list);
    }

    public async Task<IActionResult> Detail(Guid id, CancellationToken ct = default)
    {
        var ticket = await _ticketService.GetByIdAsync(id, ct);
        if (ticket == null) return NotFound();
        return View(ticket);
    }
}
