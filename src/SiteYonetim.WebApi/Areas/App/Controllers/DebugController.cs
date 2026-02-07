using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SiteYonetim.Domain.Interfaces;
using SiteYonetim.Infrastructure.Data;

namespace SiteYonetim.WebApi.Areas.App.Controllers;

/// <summary>
/// Bildirim sorunlarını tespit etmek için geçici tanılama sayfası.
/// Sorun çözüldükten sonra silinebilir.
/// </summary>
[Area("App")]
[Authorize]
public class DebugController : Controller
{
    private readonly ISiteService _siteService;
    private readonly IPaidExpenseNotificationService _notificationService;
    private readonly SiteYonetimDbContext _db;

    public DebugController(ISiteService siteService, IPaidExpenseNotificationService notificationService, SiteYonetimDbContext db)
    {
        _siteService = siteService;
        _notificationService = notificationService;
        _db = db;
    }

    public async Task<IActionResult> OverdueAidat(CancellationToken ct = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            ViewBag.Error = "Kullanıcı bilgisi alınamadı.";
            return View();
        }

        var now = DateTime.Now;
        var sites = await _siteService.GetUserSitesAsync(userId, ct);
        var siteIds = sites.Select(s => s.Id).ToList();

        var ocak2026Count = await _db.Incomes
            .Where(i => siteIds.Contains(i.SiteId) && !i.IsDeleted && i.Year == 2026 && i.Month == 1)
            .CountAsync(ct);

        var ocak2026Bekleyen = await _db.Incomes
            .Where(i => siteIds.Contains(i.SiteId) && !i.IsDeleted && i.Year == 2026 && i.Month == 1)
            .ToListAsync(ct);

        var paidByIncome = await _db.Payments
            .Where(p => p.IncomeId != null && !p.IsDeleted)
            .GroupBy(p => p.IncomeId!.Value)
            .Select(g => new { IncomeId = g.Key, Paid = g.Sum(p => p.Amount) })
            .ToDictionaryAsync(x => x.IncomeId, x => x.Paid, ct);

        var today = DateTime.Today;
        var bekleyenList = ocak2026Bekleyen
            .Where(i => paidByIncome.GetValueOrDefault(i.Id, 0m) < i.Amount)
            .Select(i => new
            {
                i.Id, i.SiteId, i.Year, i.Month, i.PaymentEndDate,
                Paid = paidByIncome.GetValueOrDefault(i.Id, 0m),
                Remaining = i.Amount - paidByIncome.GetValueOrDefault(i.Id, 0m),
                IsOverdue = i.PaymentEndDate < today
            })
            .ToList();
        var bekleyenView = bekleyenList.Select(x => new { x.SiteId, x.Year, x.Month, x.PaymentEndDate, x.Remaining, x.IsOverdue }).ToList();

        var overdueFromService = siteIds.Count > 0
            ? (siteIds.Count == 1
                ? await _notificationService.GetOverdueAidatAsync(siteIds[0], ct)
                : await _notificationService.GetOverdueAidatForSitesAsync(siteIds, ct))
            : System.Array.Empty<Domain.Interfaces.OverdueAidatNotificationDto>();

        var currentYear = now.Year;
        var currentMonth = now.Month;
        var serviceQueryCount = await _db.Incomes
            .Where(i => siteIds.Contains(i.SiteId) && !i.IsDeleted
                && (i.Year < currentYear || (i.Year == currentYear && i.Month < currentMonth)))
            .CountAsync(ct);

        var serviceQueryWithSingleSite = siteIds.Count > 0
            ? await _db.Incomes
                .Where(i => i.SiteId == siteIds[0] && !i.IsDeleted
                    && (i.Year < currentYear || (i.Year == currentYear && i.Month < currentMonth)))
                .CountAsync(ct)
            : 0;

        var firstIncomeSiteId = ocak2026Bekleyen.FirstOrDefault()?.SiteId;
        var siteIdMatches = siteIds.Count > 0 && firstIncomeSiteId == siteIds[0];

        ViewBag.ServiceQueryCount = serviceQueryCount;
        ViewBag.ServiceQueryWithSingleSite = serviceQueryWithSingleSite;
        ViewBag.FirstIncomeSiteId = firstIncomeSiteId;
        ViewBag.PassedSiteId = siteIds.Count > 0 ? siteIds[0] : (Guid?)null;
        ViewBag.SiteIdMatches = siteIdMatches;
        ViewBag.Now = now;
        ViewBag.Today = today;
        ViewBag.CurrentYear = currentYear;
        ViewBag.CurrentMonth = currentMonth;
        ViewBag.Sites = sites;
        ViewBag.SiteIds = siteIds;
        ViewBag.Ocak2026Count = ocak2026Count;
        ViewBag.BekleyenCount = bekleyenList.Count;
        ViewBag.BekleyenList = bekleyenView;
        ViewBag.OverdueFromServiceCount = overdueFromService.Count;
        ViewBag.OverdueFromService = overdueFromService;
        return View();
    }
}
