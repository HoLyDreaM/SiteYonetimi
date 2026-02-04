using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteYonetim.Domain.Interfaces;

namespace SiteYonetim.WebApi.Areas.App.Controllers;

[Area("App")]
[Authorize]
public class ProfileController : Controller
{
    private readonly IAuthService _auth;

    public ProfileController(IAuthService auth) => _auth = auth;

    private Guid GetUserId()
    {
        var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(id, out var uid) ? uid : Guid.Empty;
    }

    public IActionResult Index()
    {
        ViewBag.Email = User.FindFirst(ClaimTypes.Email)?.Value ?? "";
        ViewBag.FullName = User.FindFirst(ClaimTypes.Name)?.Value ?? "";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
        {
            TempData["PasswordError"] = "Mevcut şifre ve yeni şifre gerekli.";
            return RedirectToAction(nameof(Index));
        }
        if (newPassword != confirmPassword)
        {
            TempData["PasswordError"] = "Yeni şifre ve tekrarı eşleşmiyor.";
            return RedirectToAction(nameof(Index));
        }
        if (newPassword.Length < 6)
        {
            TempData["PasswordError"] = "Yeni şifre en az 6 karakter olmalı.";
            return RedirectToAction(nameof(Index));
        }
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            TempData["PasswordError"] = "Oturum bilgisi bulunamadı.";
            return RedirectToAction(nameof(Index));
        }
        var result = await _auth.ChangePasswordAsync(userId, currentPassword, newPassword, ct);
        if (result == ChangePasswordResult.Success)
        {
            TempData["PasswordSuccess"] = "Şifreniz başarıyla güncellendi.";
            return RedirectToAction(nameof(Index));
        }
        TempData["PasswordError"] = result == ChangePasswordResult.WrongPassword
            ? "Mevcut şifre hatalı."
            : "İşlem başarısız.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeEmail(string newEmail, string password, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(newEmail) || string.IsNullOrWhiteSpace(password))
        {
            TempData["EmailError"] = "Yeni e-posta ve mevcut şifre gerekli.";
            return RedirectToAction(nameof(Index));
        }
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            TempData["EmailError"] = "Oturum bilgisi bulunamadı.";
            return RedirectToAction(nameof(Index));
        }
        var result = await _auth.ChangeEmailAsync(userId, newEmail.Trim(), password, ct);
        if (result == ChangeEmailResult.Success)
        {
            await UpdateCookieClaims(newEmail.Trim(), User.FindFirst(ClaimTypes.Name)?.Value ?? "");
            TempData["EmailSuccess"] = "E-posta adresiniz güncellendi. Yeni e-posta ile giriş yapabilirsiniz.";
            return RedirectToAction(nameof(Index));
        }
        TempData["EmailError"] = result switch
        {
            ChangeEmailResult.WrongPassword => "Mevcut şifre hatalı.",
            ChangeEmailResult.EmailAlreadyExists => "Bu e-posta adresi zaten kullanılıyor.",
            _ => "İşlem başarısız."
        };
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(string fullName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            TempData["ProfileError"] = "Ad soyad gerekli.";
            return RedirectToAction(nameof(Index));
        }
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            TempData["ProfileError"] = "Oturum bilgisi bulunamadı.";
            return RedirectToAction(nameof(Index));
        }
        if (await _auth.UpdateProfileAsync(userId, fullName, ct))
        {
            await UpdateCookieClaims(User.FindFirst(ClaimTypes.Email)?.Value ?? "", fullName.Trim());
            TempData["ProfileSuccess"] = "Profil bilgileriniz güncellendi.";
            return RedirectToAction(nameof(Index));
        }
        TempData["ProfileError"] = "İşlem başarısız.";
        return RedirectToAction(nameof(Index));
    }

    private async Task UpdateCookieClaims(string email, string fullName)
    {
        var identity = (System.Security.Claims.ClaimsIdentity)User.Identity!;
        var emailClaim = identity.FindFirst(ClaimTypes.Email);
        var nameClaim = identity.FindFirst(ClaimTypes.Name);
        if (emailClaim != null) identity.RemoveClaim(emailClaim);
        if (nameClaim != null) identity.RemoveClaim(nameClaim);
        identity.AddClaim(new Claim(ClaimTypes.Email, email));
        identity.AddClaim(new Claim(ClaimTypes.Name, fullName));
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) });
    }
}
