using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using SiteYonetim.Domain.Interfaces;

namespace SiteYonetim.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        var host = _config["Email:SmtpHost"];
        var port = _config.GetValue<int>("Email:SmtpPort", 587);
        var user = _config["Email:Username"];
        var pass = _config["Email:Password"];
        var from = _config["Email:From"] ?? user ?? "noreply@siteyonetim.local";

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(to))
        {
            _logger.LogWarning("E-posta gönderilemedi: SMTP veya alıcı adresi tanımlı değil.");
            return;
        }

        try
        {
            var msg = new MimeMessage();
            msg.From.Add(MailboxAddress.Parse(from));
            msg.To.Add(MailboxAddress.Parse(to));
            msg.Subject = subject;
            msg.Body = new TextPart("html") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTlsWhenAvailable, ct);
            if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass))
                await client.AuthenticateAsync(user, pass, ct);
            await client.SendAsync(msg, ct);
            await client.DisconnectAsync(true, ct);
            _logger.LogInformation("E-posta gönderildi: {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "E-posta gönderilemedi: {To}", to);
        }
    }

    public async Task SendWithSiteSmtpAsync(string to, string subject, string body, string? smtpHost, int? smtpPort, string? smtpUser, string? smtpPass, CancellationToken ct = default)
    {
        var host = smtpHost ?? _config["Email:SmtpHost"];
        var port = smtpPort ?? _config.GetValue<int>("Email:SmtpPort", 587);
        var user = smtpUser ?? _config["Email:Username"];
        var pass = smtpPass ?? _config["Email:Password"];
        var from = _config["Email:From"] ?? user ?? "noreply@siteyonetim.local";

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(to))
        {
            _logger.LogWarning("E-posta gönderilemedi: SMTP veya alıcı adresi tanımlı değil.");
            return;
        }

        try
        {
            var msg = new MimeMessage();
            msg.From.Add(MailboxAddress.Parse(from));
            msg.To.Add(MailboxAddress.Parse(to));
            msg.Subject = subject;
            msg.Body = new TextPart("html") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTlsWhenAvailable, ct);
            if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass))
                await client.AuthenticateAsync(user, pass, ct);
            await client.SendAsync(msg, ct);
            await client.DisconnectAsync(true, ct);
            _logger.LogInformation("E-posta gönderildi: {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "E-posta gönderilemedi: {To}", to);
        }
    }
}
