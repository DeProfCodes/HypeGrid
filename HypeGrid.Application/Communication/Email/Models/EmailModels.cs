namespace HypeGrid.Application.Communication.Email.Models;

/// <summary>A transport-level email message.</summary>
public sealed class EmailMessage
{
    public string ToEmail { get; set; } = string.Empty;
    public string? ToName { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string PlainTextBody { get; set; } = string.Empty;
    public EmailSender Sender { get; set; } = EmailSender.Default;

    /// <summary>
    /// Blind-copy recipients. Used for internal admin notifications so a copy can
    /// be routed to extra inboxes without exposing those addresses to the primary
    /// recipient. Never populated on customer-facing emails.
    /// </summary>
    public List<string> Bcc { get; set; } = new();
}

/// <summary>SMTP options for a single logical sender identity.</summary>
public sealed class EmailSenderOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public bool EnableSsl { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(FromEmail);
}
