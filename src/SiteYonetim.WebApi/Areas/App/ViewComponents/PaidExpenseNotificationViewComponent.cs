using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SiteYonetim.Domain.Interfaces;

namespace SiteYonetim.WebApi.Areas.App.ViewComponents;

public class PaidExpenseNotificationViewComponent : ViewComponent
{
    private readonly IPaidExpenseNotificationService _notificationService;
    private readonly IReportService _reportService;
    private readonly ISiteService _siteService;

    public PaidExpenseNotificationViewComponent(IPaidExpenseNotificationService notificationService, IReportService reportService, ISiteService siteService)
    {
        _notificationService = notificationService;
        _reportService = reportService;
        _siteService = siteService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var siteIdStr = Request.Query["siteId"].FirstOrDefault()
            ?? ViewData["SiteId"]?.ToString()
            ?? (HttpContext.Items["SelectedSiteId"] is Guid g ? g.ToString() : null);
        var siteIds = new List<Guid>();

        if (!string.IsNullOrEmpty(siteIdStr) && Guid.TryParse(siteIdStr, out var parsedId))
            siteIds.Add(parsedId);
        else
        {
            // Site seçili değilse tüm kullanıcı sitelerinden bildirim topla (hiçbir site kaçmasın)
            var userIdClaim = (User as ClaimsPrincipal)?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
            {
                var sites = await _siteService.GetUserSitesAsync(userId, HttpContext.RequestAborted);
                siteIds.AddRange(sites.Select(s => s.Id));
            }
        }

        if (siteIds.Count == 0)
            return View(new NotificationViewModel(Array.Empty<OverdueExpenseNotificationDto>(), Array.Empty<DebtorDto>()));

        var overdueExpenses = siteIds.Count == 1
            ? await _notificationService.GetOverdueExpensesAsync(siteIds[0], HttpContext.RequestAborted)
            : await _notificationService.GetOverdueExpensesForSitesAsync(siteIds, HttpContext.RequestAborted);

        // Borçlular verisini kullan (ReportService.GetDebtorsAsync - borçlular sayfasıyla aynı mantık)
        var debtors = new List<DebtorDto>();
        foreach (var sid in siteIds)
        {
            var list = await _reportService.GetDebtorsAsync(sid, HttpContext.RequestAborted);
            debtors.AddRange(list);
        }
        debtors = debtors.OrderBy(d => d.OldestDebtDate ?? DateTime.MaxValue).Take(20).ToList();

        ViewData["SiteId"] = siteIds.First().ToString();
        return View(new NotificationViewModel(overdueExpenses, debtors));
    }
}

public record NotificationViewModel(
    IReadOnlyList<OverdueExpenseNotificationDto> OverdueExpenses,
    IReadOnlyList<DebtorDto> Debtors
);
