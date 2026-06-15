using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using HypeGrid.Infrastructure.Configuration;

namespace HypeGrid.Infrastructure.Communications.Email;

/// <summary>
/// Boot-time diagnostic: logs per-sender readiness once at startup so a
/// misconfigured environment is obvious in the startup log rather than at the
/// first lead submission. Logs field NAMES / readiness only — never secrets.
/// </summary>
public sealed class EmailSenderConfigReporter : IHostedService
{
    private readonly EmailSenderSettings _settings;
    private readonly ILogger<EmailSenderConfigReporter> _logger;

    public EmailSenderConfigReporter(IOptions<EmailSenderSettings> settings, ILogger<EmailSenderConfigReporter> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_settings.Senders.Count == 0)
        {
            _logger.LogWarning("[EmailConfig] No SMTP senders configured. Outbound email is disabled.");
            return Task.CompletedTask;
        }

        foreach (var (name, options) in _settings.Senders)
        {
            if (!options.IsConfigured)
            {
                // Empty Host/FromEmail -> the mapper routes this identity to 'Default'.
                // Expected for senders we deliberately leave unconfigured (e.g. NoReply,
                // Campaigns, Creators sharing the Default mailbox) — not an error.
                _logger.LogInformation(
                    "[EmailConfig] Sender '{Sender}' not configured (no host/from) — falls back to 'Default'.", name);
            }
            else if (string.IsNullOrWhiteSpace(options.Password))
            {
                // Configured to send on its own, but no password supplied: sends WILL fail
                // until the password env var is provided. Never logs the secret itself.
                _logger.LogWarning(
                    "[EmailConfig] Sender '{Sender}' configured ({Host}:{Port}) but PASSWORD missing — sends will fail until env var {PasswordKey} is supplied.",
                    name, options.Host, options.Port, $"EmailProviders__Senders__{name}__Password");
            }
            else
            {
                _logger.LogInformation(
                    "[EmailConfig] Sender '{Sender}' ready ({Host}:{Port}, ssl={EnableSsl}).",
                    name, options.Host, options.Port, options.EnableSsl);
            }
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
