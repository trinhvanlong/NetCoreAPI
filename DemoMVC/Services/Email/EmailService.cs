using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace DemoMVC.Services.Email;

public sealed class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAsync(
        string recipient,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recipient) ||
            string.IsNullOrWhiteSpace(subject) ||
            string.IsNullOrWhiteSpace(htmlBody))
        {
            throw new ArgumentException("Thông tin email không hợp lệ.", nameof(recipient));
        }

        if (string.IsNullOrWhiteSpace(_settings.Host) ||
            string.IsNullOrWhiteSpace(_settings.Username) ||
            string.IsNullOrWhiteSpace(_settings.Password) ||
            string.IsNullOrWhiteSpace(_settings.SenderEmail))
        {
            _logger.LogWarning("Không thể gửi email vì cấu hình SMTP chưa đầy đủ.");
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(recipient);

        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.EnableSsl,
            Credentials = new NetworkCredential(_settings.Username, _settings.Password)
        };

        try
        {
            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Đã gửi email tới người dùng {RecipientMasked}.", MaskRecipient(recipient));
        }
        catch (Exception exception) when (
            exception is SmtpException ||
            exception is IOException ||
            exception is OperationCanceledException)
        {
            _logger.LogError(exception, "Gửi email thất bại tới người dùng {RecipientMasked}.", MaskRecipient(recipient));
        }
    }

    private static string MaskRecipient(string recipient)
    {
        var at = recipient.IndexOf('@');
        if (at <= 1) return "***";
        return recipient[0] + "***@" + recipient[(at + 1)..];
    }
}
