using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SiteYonetim.WebApi.Filters;

/// <summary>
/// App alanına sadece yönetici rolleri (SuperAdmin, SiteManager, Accountant) erişebilir.
/// Resident (sakin) rolü sadece Destek sayfasına erişebilir.
/// </summary>
public class AppAreaAuthorizationFilter : IAsyncAuthorizationFilter
{
    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var area = context.RouteData.Values["area"]?.ToString();
        if (area != "App")
            return Task.CompletedTask;

        if (!(context.HttpContext.User.Identity?.IsAuthenticated ?? false))
            return Task.CompletedTask; // [Authorize] zaten yönlendirecek

        var roleClaim = context.HttpContext.User.FindFirst(ClaimTypes.Role)?.Value
            ?? context.HttpContext.User.FindFirst("role")?.Value;
        if (string.IsNullOrEmpty(roleClaim))
            return Task.CompletedTask;

        // "Resident" veya "3" (enum değeri) ise App paneline erişim engelle
        if (roleClaim == "Resident" || roleClaim == "3")
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", new { area = "" });
        }

        return Task.CompletedTask;
    }
}
