using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteYonetim.Domain.Interfaces;

namespace SiteYonetim.WebApi.Areas.App.Controllers;

[Area("App")]
[Authorize]
public class ExpenseSharesController : Controller
{
    private readonly IExpenseShareService _expenseShareService;
    private readonly ISiteService _siteService;

    public ExpenseSharesController(IExpenseShareService expenseShareService, ISiteService siteService)
    {
        _expenseShareService = expenseShareService;
        _siteService = siteService;
    }

    public async Task<IActionResult> Index(Guid? siteId, int? status, CancellationToken ct)
    {
        if (!siteId.HasValue)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return RedirectToAction("Login", "Account", new { area = "" });
            var sites = await _siteService.GetUserSitesAsync(userId, ct);
            ViewBag.Sites = sites;
            ViewBag.PageTitle = "Borçlar - Site Seçin";
            return View("SelectSite");
        }
        var list = await _expenseShareService.GetBySiteIdAsync(siteId.Value, null, status, ct);
        ViewBag.SiteId = siteId;
        ViewBag.Status = status;
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyLateFees(Guid siteId, CancellationToken ct)
    {
        await _expenseShareService.ApplyLateFeesAsync(siteId, ct);
        return RedirectToAction(nameof(Index), new { area = "App", siteId });
    }
}
