using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SiteYonetim.WebApi.Areas.App.Filters;

/// <summary>
/// Seçili siteyi cookie'de saklar ve ViewBag.SiteId'yi doldurur (Layout için).
/// </summary>
public class SelectedSiteFilter : IActionFilter
{
    private const string CookieName = "SelectedSiteId";

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.RouteData.Values["area"]?.ToString() != "App")
            return;

        if (context.HttpContext.Request.Query.TryGetValue("siteId", out var siteIdVal) &&
            Guid.TryParse(siteIdVal, out var siteId))
        {
            context.HttpContext.Response.Cookies.Append(CookieName, siteId.ToString(), new CookieOptions
            {
                Path = "/",
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromDays(30)
            });
            context.HttpContext.Items["SelectedSiteId"] = siteId;
        }
        else if (context.HttpContext.Request.Cookies.TryGetValue(CookieName, out var cookieVal) &&
                 Guid.TryParse(cookieVal, out var cookieSiteId))
        {
            context.HttpContext.Items["SelectedSiteId"] = cookieSiteId;
        }

        var controller = context.Controller as Controller;
        if (controller != null && context.HttpContext.Items.TryGetValue("SelectedSiteId", out var siteIdObj) && siteIdObj is Guid siteGuid)
        {
            controller.ViewBag.SiteId = siteGuid;
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
