using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using HypeGrid.Application.Communication.Email.Interfaces;
using HypeGrid.Application.Communication.Email.Models;
using HypeGrid.Shared.Errors;
using HypeGrid.Shared.Results;

namespace HypeGrid.Infrastructure.Communications.Email;

/// <summary>
/// SMTP email provider built on <see cref="System.Net.Mail.SmtpClient"/>. Sends
/// a multipart (HTML + plain-text) message using the resolved sender identity.
/// Returns a <see cref="Result"/> rather than throwing — there is no retry or
/// persistence in this phase (matches the ZansiHustle posture).
/// </summary>
public sealed class SmtpEmailProvider : IEmailProvider
{
    private readonly IEmailSenderMapper _senderMapper;
    private readonly ILogger<SmtpEmailProvider> _logger;

    public SmtpEmailProvider(IEmailSenderMapper senderMapper, ILogger<SmtpEmailProvider> logger)
    {
        _senderMapper = senderMapper;
        _logger = logger;
    }

    public async Task<Result> SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var options = _senderMapper.Resolve(message.Sender);
        if (!options.IsConfigured)
        {
            _logger.LogWarning("SMTP sender '{Sender}' is not configured; skipping email to {Recipient}.",
                message.Sender, message.ToEmail);
            return Result.Failure(ErrorCodes.ProviderNotConfigured, "Email sender is not configured.");
        }

        try
        {
            using var mail = new MailMessage
            {
                From = new MailAddress(options.FromEmail, options.FromName),
                Subject = message.Subject,
                Body = message.HtmlBody,
                IsBodyHtml = true
            };
            mail.To.Add(string.IsNullOrWhiteSpace(message.ToName)
                ? new MailAddress(message.ToEmail)
                : new MailAddress(message.ToEmail, message.ToName));

            // Blind-copy recipients (internal notifications only). Skipped silently
            // if an address is malformed so one bad entry can't fail the send.
            foreach (var bcc in message.Bcc)
            {
                if (string.IsNullOrWhiteSpace(bcc)) continue;
                try { mail.Bcc.Add(new MailAddress(bcc.Trim())); }
                catch (FormatException) { _logger.LogWarning("Skipping malformed BCC address on email to {Recipient}.", message.ToEmail); }
            }

            // Plain-text alternate view improves deliverability + accessibility.
            if (!string.IsNullOrWhiteSpace(message.PlainTextBody))
            {
                var textView = AlternateView.CreateAlternateViewFromString(
                    message.PlainTextBody, null, "text/plain");
                var htmlView = AlternateView.CreateAlternateViewFromString(
                    message.HtmlBody, null, "text/html");
                mail.AlternateViews.Add(textView);
                mail.AlternateViews.Add(htmlView);
            }

            using var client = new SmtpClient(options.Host, options.Port)
            {
                EnableSsl = options.EnableSsl,
                Credentials = string.IsNullOrWhiteSpace(options.Username)
                    ? CredentialCache.DefaultNetworkCredentials
                    : new NetworkCredential(options.Username, options.Password)
            };

            await client.SendMailAsync(mail, ct);
            _logger.LogInformation("Email sent to {Recipient} via {Sender}.", message.ToEmail, message.Sender);
            return Result.Success();
        }
        catch (Exception ex)
        {
            // Never log the body or credentials — only the failing recipient.
            _logger.LogError(ex, "SMTP send failed to {Recipient} via {Sender}.", message.ToEmail, message.Sender);
            return Result.Failure(ErrorCodes.EmailSendFailed, "Failed to send email.");
        }
    }
}
