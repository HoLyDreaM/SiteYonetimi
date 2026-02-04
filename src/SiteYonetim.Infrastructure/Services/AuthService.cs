using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SiteYonetim.Domain;
using SiteYonetim.Domain.Entities;
using SiteYonetim.Domain.Interfaces;
using SiteYonetim.Infrastructure.Data;

namespace SiteYonetim.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly SiteYonetimDbContext _db;
    private readonly JwtSettings _jwt;

    public AuthService(SiteYonetimDbContext db, IOptions<JwtSettings> jwt)
    {
        _db = db;
        _jwt = jwt.Value;
    }

    public async Task<AuthResult?> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Include(x => x.UserSites)
            .FirstOrDefaultAsync(x => x.Email == email && !x.IsDeleted && x.IsActive && x.IsApproved, ct);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        var siteIds = user.UserSites?.Where(x => !x.IsDeleted).Select(x => x.SiteId).ToList() ?? new List<Guid>();
        var (accessToken, expiresAt) = GenerateAccessToken(user, siteIds);
        var refreshToken = GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays);
        await _db.SaveChangesAsync(ct);

        return new AuthResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = (int)user.Role,
            SiteIds = siteIds
        };
    }

    public async Task<AuthResult?> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Include(x => x.UserSites)
            .FirstOrDefaultAsync(x => x.RefreshToken == refreshToken && !x.IsDeleted && x.IsActive && x.IsApproved
                && x.RefreshTokenExpiry != null && x.RefreshTokenExpiry > DateTime.UtcNow, ct);
        if (user == null) return null;

        var siteIds = user.UserSites?.Where(x => !x.IsDeleted).Select(x => x.SiteId).ToList() ?? new List<Guid>();
        var (accessToken, expiresAt) = GenerateAccessToken(user, siteIds);
        var newRefresh = GenerateRefreshToken();
        user.RefreshToken = newRefresh;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays);
        await _db.SaveChangesAsync(ct);

        return new AuthResult
        {
            AccessToken = accessToken,
            RefreshToken = newRefresh,
            ExpiresAt = expiresAt,
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = (int)user.Role,
            SiteIds = siteIds
        };
    }

    public async Task<(bool Success, bool IsFirstUser)> RegisterAsync(string email, string password, string fullName, CancellationToken ct = default)
    {
        if (await _db.Users.AnyAsync(x => x.Email == email && !x.IsDeleted, ct))
            return (false, false);
        var isFirstUser = !await _db.Users.AnyAsync(x => !x.IsDeleted, ct);
        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12)),
            FullName = fullName,
            Role = isFirstUser ? UserRole.SiteManager : UserRole.Resident,
            IsActive = true,
            IsApproved = isFirstUser
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return (true, isFirstUser);
    }

    public async Task<bool> IsUserPendingApprovalAsync(string email, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email && !x.IsDeleted, ct);
        return user != null && !user.IsApproved;
    }

    public async Task<ChangePasswordResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted, ct);
        if (user == null) return ChangePasswordResult.UserNotFound;
        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            return ChangePasswordResult.WrongPassword;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, BCrypt.Net.BCrypt.GenerateSalt(12));
        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        await _db.SaveChangesAsync(ct);
        return ChangePasswordResult.Success;
    }

    public async Task<ChangeEmailResult> ChangeEmailAsync(Guid userId, string newEmail, string password, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted, ct);
        if (user == null) return ChangeEmailResult.UserNotFound;
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return ChangeEmailResult.WrongPassword;
        var email = newEmail.Trim();
        if (await _db.Users.AnyAsync(x => x.Email == email && x.Id != userId && !x.IsDeleted, ct))
            return ChangeEmailResult.EmailAlreadyExists;
        user.Email = email;
        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        await _db.SaveChangesAsync(ct);
        return ChangeEmailResult.Success;
    }

    public async Task<bool> UpdateProfileAsync(Guid userId, string fullName, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted, ct);
        if (user == null) return false;
        user.FullName = fullName.Trim();
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private (string accessToken, DateTime expiresAt) GenerateAccessToken(User user, List<Guid> siteIds)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_jwt.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new("role", ((int)user.Role).ToString())
        };
        foreach (var siteId in siteIds)
            claims.Add(new Claim("site_id", siteId.ToString()));
        var token = new JwtSecurityToken(
            _jwt.Issuer,
            _jwt.Audience,
            claims,
            expires: expiresAt,
            signingCredentials: creds
        );
        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}
