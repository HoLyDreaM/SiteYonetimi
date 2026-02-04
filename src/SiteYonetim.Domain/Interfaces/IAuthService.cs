namespace SiteYonetim.Domain.Interfaces;

public interface IAuthService
{
    Task<AuthResult?> LoginAsync(string email, string password, CancellationToken ct = default);
    Task<AuthResult?> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<(bool Success, bool IsFirstUser)> RegisterAsync(string email, string password, string fullName, CancellationToken ct = default);
    Task<bool> IsUserPendingApprovalAsync(string email, CancellationToken ct = default);
    /// <summary>Şifre değiştirir. Mevcut şifre doğrulanmalı.</summary>
    Task<ChangePasswordResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct = default);
    /// <summary>E-posta değiştirir. Mevcut şifre doğrulanmalı.</summary>
    Task<ChangeEmailResult> ChangeEmailAsync(Guid userId, string newEmail, string password, CancellationToken ct = default);
    /// <summary>Ad soyad günceller.</summary>
    Task<bool> UpdateProfileAsync(Guid userId, string fullName, CancellationToken ct = default);
}

public enum ChangePasswordResult { Success, WrongPassword, UserNotFound }
public enum ChangeEmailResult { Success, WrongPassword, UserNotFound, EmailAlreadyExists }

public class AuthResult
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int Role { get; set; }
    public IReadOnlyList<Guid> SiteIds { get; set; } = Array.Empty<Guid>();
}
