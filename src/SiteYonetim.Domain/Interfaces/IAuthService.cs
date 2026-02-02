namespace SiteYonetim.Domain.Interfaces;

public interface IAuthService
{
    Task<AuthResult?> LoginAsync(string email, string password, CancellationToken ct = default);
    Task<AuthResult?> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<(bool Success, bool IsFirstUser)> RegisterAsync(string email, string password, string fullName, CancellationToken ct = default);
    Task<bool> IsUserPendingApprovalAsync(string email, CancellationToken ct = default);
}

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
