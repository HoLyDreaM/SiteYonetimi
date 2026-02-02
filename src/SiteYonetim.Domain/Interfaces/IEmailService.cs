namespace SiteYonetim.Domain.Interfaces;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct = default);
    /// <summary>Site bazlı SMTP ayarları ile gönderir. Site ayarları yoksa appsettings kullanılır.</summary>
    Task SendWithSiteSmtpAsync(string to, string subject, string body, string? smtpHost, int? smtpPort, string? smtpUser, string? smtpPass, CancellationToken ct = default);
}
