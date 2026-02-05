using Microsoft.AspNetCore.Mvc;
using SiteYonetim.Domain.Interfaces;

namespace SiteYonetim.WebApi.Areas.App.ViewComponents;

public class PaidExpenseNotificationViewComponent : ViewComponent
{
    private readonly IPaidExpenseNotificationService _notificationService;

    public PaidExpenseNotificationViewComponent(IPaidExpenseNotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var siteIdStr = Request.Query["siteId"].FirstOrDefault() ?? ViewData["SiteId"]?.ToString();
        if (string.IsNullOrEmpty(siteIdStr) || !Guid.TryParse(siteIdStr, out var siteId))
            return View(Array.Empty<PaidExpenseNotificationDto>());

        var items = await _notificationService.GetRecentlyPaidExpensesAsync(siteId, 30, HttpContext.RequestAborted);
        ViewData["SiteId"] = siteId.ToString();
        return View(items);
    }
}
