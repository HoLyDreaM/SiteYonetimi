using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiteYonetim.Domain.Interfaces;
using SiteYonetim.WebApi.Models;

namespace SiteYonetim.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(request.Email, request.Password, ct);
        if (result == null) return Unauthorized(new { message = "E-posta veya şifre hatalı." });
        return Ok(new AuthResponse
        {
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken,
            ExpiresAt = result.ExpiresAt,
            UserId = result.UserId,
            Email = result.Email,
            FullName = result.FullName,
            Role = result.Role,
            SiteIds = result.SiteIds
        });
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var (ok, isFirst) = await _auth.RegisterAsync(request.Email, request.Password, request.FullName, ct);
        if (!ok) return BadRequest(new { message = "Bu e-posta adresi zaten kayıtlı." });
        return Ok(new { message = isFirst ? "Kayıt başarılı. İlk kullanıcı olarak otomatik onaylandınız." : "Kayıt başarılı. Yönetici onayından sonra giriş yapabileceksiniz." });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await _auth.RefreshTokenAsync(request.RefreshToken, ct);
        if (result == null) return Unauthorized(new { message = "Geçersiz veya süresi dolmuş token." });
        return Ok(new AuthResponse
        {
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken,
            ExpiresAt = result.ExpiresAt,
            UserId = result.UserId,
            Email = result.Email,
            FullName = result.FullName,
            Role = result.Role,
            SiteIds = result.SiteIds
        });
    }
}
