using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;

namespace SiteYonetim.WebApi.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly IAuthService _auth;

    public AccountController(IAuthService auth) => _auth = auth;

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl ?? Url.Content("~/App/Dashboard");
        return View();
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string email, string password, string fullName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(fullName))
        {
            ModelState.AddModelError("", "E-posta, şifre ve ad soyad gerekli.");
            return View();
        }
        var (ok, isFirst) = await _auth.RegisterAsync(email, password, fullName, ct);
        if (!ok)
        {
            ModelState.AddModelError("", "Bu e-posta adresi zaten kayıtlı.");
            return View();
        }
        TempData["Message"] = isFirst ? "Kayıt başarılı. İlk kullanıcı olarak otomatik onaylandınız, giriş yapabilirsiniz." : "Kayıt başarılı. Yönetici onayından sonra giriş yapabileceksiniz.";
        return RedirectToAction("Login");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, string? returnUrl, CancellationToken ct)
    {
        ViewData["ReturnUrl"] = returnUrl ?? Url.Content("~/App/Dashboard");
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("", "E-posta ve şifre girin.");
            return View();
        }
        var result = await _auth.LoginAsync(email, password, ct);
        if (result == null)
        {
            if (await _auth.IsUserPendingApprovalAsync(email, ct))
                ModelState.AddModelError("", "Hesabınız henüz yönetici onayı bekliyor.");
            else
                ModelState.AddModelError("", "E-posta veya şifre hatalı.");
            return View();
        }
        var role = (UserRole)result.Role;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.UserId.ToString()),
            new(ClaimTypes.Email, result.Email),
            new(ClaimTypes.Name, result.FullName),
            new(ClaimTypes.Role, role.ToString()),
            new("role", result.Role.ToString())
        };
        foreach (var siteId in result.SiteIds)
            claims.Add(new Claim("site_id", siteId.ToString()));
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) });

        // Resident (sakin) sadece Destek sayfasına erişebilir; App paneline erişemez
        var redirectTo = returnUrl ?? "/App/Dashboard";
        if (role == UserRole.Resident && (string.IsNullOrEmpty(returnUrl) || redirectTo.StartsWith("/App", StringComparison.OrdinalIgnoreCase)))
            redirectTo = "/Account/AccessDenied";
        return LocalRedirect(redirectTo);
    }

    [HttpGet]
    [Authorize]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}
