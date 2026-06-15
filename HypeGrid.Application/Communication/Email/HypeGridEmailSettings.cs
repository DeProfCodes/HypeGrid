namespace HypeGrid.Application.Communication.Email;

/// <summary>
/// Brand + routing settings for HypeGrid outbound email. Config-driven so the
/// admin/support recipient and footer branding can be set per environment
/// without code changes (TODO: confirm final addresses — see README open items).
/// </summary>
public sealed class HypeGridEmailSettings
{
    public const string SectionName = "HypeGridEmail";

    /// <summary>Internal inbox that receives new-lead notifications (primary To).</summary>
    public string AdminNotificationEmail { get; set; } = "support@hypegrid.co.za";

    /// <summary>
    /// Optional extra recipients BCC'd on internal admin/lead notifications (NOT on
    /// customer acknowledgements). Comma- or semicolon-separated to allow several,
    /// e.g. "owner@hypegrid.co.za;founder@gmail.com". Empty by default. Best set as
    /// an app-pool / env setting (HypeGridEmail__AdminNotificationBccEmails) so it
    /// can change without a redeploy.
    /// </summary>
    public string? AdminNotificationBccEmails { get; set; }

    /// <summary>Parsed, de-duplicated BCC list derived from <see cref="AdminNotificationBccEmails"/>.</summary>
    public IReadOnlyList<string> AdminNotificationBccList =>
        string.IsNullOrWhiteSpace(AdminNotificationBccEmails)
            ? Array.Empty<string>()
            : AdminNotificationBccEmails
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

    public string CompanyName { get; set; } = "HypeGrid";

    /// <summary>Legal/company line shown in email footers.</summary>
    public string CompanyLegalName { get; set; } = "HypeGrid";

    /// <summary>Brand primary colour used in template chrome.</summary>
    public string PrimaryColor { get; set; } = "#06b6d4";

    /// <summary>Public website URL (for links/footer).</summary>
    public string WebsiteUrl { get; set; } = "https://hypegrid.co.za";

    /// <summary>Admin portal base URL, used to deep-link records in admin notifications.</summary>
    public string AdminBaseUrl { get; set; } = "https://admin.hypegrid.co.za";
}
