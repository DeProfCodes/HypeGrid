using HypeGrid.Application.Communication.Email.Models;

namespace HypeGrid.Infrastructure.Configuration;

/// <summary>
/// SMTP sender pool (bound from the "EmailProviders" section). Each named slot
/// maps to a logical <c>EmailSender</c> identity. Secrets (passwords) must come
/// from environment variables / user-secrets — never committed.
/// </summary>
public sealed class EmailSenderSettings
{
    public const string SectionName = "EmailProviders";

    public Dictionary<string, EmailSenderOptions> Senders { get; set; } = new();
}
