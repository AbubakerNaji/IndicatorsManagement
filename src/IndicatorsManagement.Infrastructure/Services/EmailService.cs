using System.Net;
using System.Net.Mail;
using IndicatorsManagement.Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IndicatorsManagement.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        await SendToMultipleAsync([to], subject, htmlBody);
    }

    public async Task SendToMultipleAsync(IEnumerable<string> recipients, string subject, string htmlBody)
    {
        var smtpHost = _config["Smtp:Host"];
        if (string.IsNullOrEmpty(smtpHost))
        {
            _logger.LogWarning("SMTP not configured — skipping email: {Subject}", subject);
            return;
        }

        var port = _config.GetValue("Smtp:Port", 587);
        var username = _config["Smtp:Username"] ?? "";
        var password = _config["Smtp:Password"] ?? "";
        var fromEmail = _config["Smtp:FromEmail"] ?? "noreply@indicators.gov";
        var fromName = _config["Smtp:FromName"] ?? "نظام إدارة المؤشرات";
        var enableSsl = _config.GetValue("Smtp:EnableSsl", true);

        using var client = new SmtpClient(smtpHost, port)
        {
            Credentials = new NetworkCredential(username, password),
            EnableSsl = enableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 30_000
        };

        foreach (var recipient in recipients)
        {
            try
            {
                var message = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };
                message.To.Add(recipient);

                await client.SendMailAsync(message);
                _logger.LogInformation("Email sent to {Recipient}: {Subject}", recipient, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Recipient}: {Subject}", recipient, subject);
            }
        }
    }
}
