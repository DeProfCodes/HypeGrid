using System.Net;
using System.Text;
using HypeGrid.Domain.Leads;

namespace HypeGrid.Application.Communication.Email.Templates;

/// <summary>
/// Pure-C# email template builders. Each method returns a
/// (Subject, HtmlBody, PlainTextBody) tuple. No template engine — every
/// interpolated user value is passed through <see cref="WebUtility.HtmlEncode"/>
/// so user-controlled fields cannot inject markup. Shared chrome is produced by
/// <see cref="Wrap"/>. This mirrors the ZansiHustle template contract described
/// in COMMUNICATION_SYSTEM_REPORT.md, rewritten with HypeGrid branding.
/// </summary>
public sealed class HypeGridEmailTemplates
{
    private readonly HypeGridEmailSettings _settings;

    public HypeGridEmailTemplates(HypeGridEmailSettings settings) => _settings = settings;

    // ── Public-website customer acknowledgements ────────────────────────────

    public (string Subject, string Html, string Text) ContactAcknowledgement(string fullName)
    {
        var name = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(fullName) ? "there" : fullName);
        var body =
            $"<p>Hi {name},</p>" +
            "<p>Thank you for contacting HypeGrid.</p>" +
            "<p>We&rsquo;ve received your message and our team will get back to you soon.</p>" +
            "<p>Regards,<br/>The HypeGrid Team</p>";
        var text =
            $"Hi {fullName},\n\nThank you for contacting HypeGrid.\n\n" +
            "We've received your message and our team will get back to you soon.\n\nRegards,\nThe HypeGrid Team";
        return ("We received your message — HypeGrid", Wrap("We received your message", body), text);
    }

    public (string Subject, string Html, string Text) CampaignRequestAcknowledgement(string fullName)
    {
        var name = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(fullName) ? "there" : fullName);
        var body =
            $"<p>Hi {name},</p>" +
            "<p>Thank you for starting a campaign request with HypeGrid.</p>" +
            "<p>We&rsquo;ve received your details and our team will review your campaign needs. " +
            "We&rsquo;ll contact you soon to discuss the right approach for your brand, song, event, product, or launch.</p>" +
            "<p>Regards,<br/>The HypeGrid Team</p>";
        var text =
            $"Hi {fullName},\n\nThank you for starting a campaign request with HypeGrid.\n\n" +
            "We've received your details and our team will review your campaign needs. " +
            "We'll contact you soon to discuss the right approach for your brand, song, event, product, or launch.\n\n" +
            "Regards,\nThe HypeGrid Team";
        return ("Your HypeGrid campaign request was received", Wrap("Campaign request received", body), text);
    }

    public (string Subject, string Html, string Text) CreatorApplicationAcknowledgement(string fullName)
    {
        var name = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(fullName) ? "there" : fullName);
        var body =
            $"<p>Hi {name},</p>" +
            "<p>Thank you for applying to join the HypeGrid Creator Network.</p>" +
            "<p>We&rsquo;ve received your application and our team will review it. " +
            "If your profile fits upcoming campaigns, we&rsquo;ll contact you with next steps.</p>" +
            "<p>Regards,<br/>The HypeGrid Team</p>";
        var text =
            $"Hi {fullName},\n\nThank you for applying to join the HypeGrid Creator Network.\n\n" +
            "We've received your application and our team will review it. " +
            "If your profile fits upcoming campaigns, we'll contact you with next steps.\n\n" +
            "Regards,\nThe HypeGrid Team";
        return ("Your HypeGrid creator application was received", Wrap("Creator application received", body), text);
    }

    public (string Subject, string Html, string Text) AlertWelcome(string? fullName)
    {
        var name = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(fullName) ? "there" : fullName!);
        var body =
            $"<p>Hi {name},</p>" +
            "<p>Thanks for joining HypeGrid Alerts.</p>" +
            "<p>We&rsquo;ll use your preferences to share relevant deals, specials, campaigns, events, " +
            "and promotions from HypeGrid.</p>" +
            "<p>You can opt out anytime.</p>" +
            "<p>Regards,<br/>The HypeGrid Team</p>";
        var text =
            $"Hi {fullName},\n\nThanks for joining HypeGrid Alerts.\n\n" +
            "We'll use your preferences to share relevant deals, specials, campaigns, events, and promotions from HypeGrid.\n\n" +
            "You can opt out anytime.\n\nRegards,\nThe HypeGrid Team";
        return ("You're on the HypeGrid Deals list", Wrap("You're on the HypeGrid Deals list", body), text);
    }

    public (string Subject, string Html, string Text) NewsletterWelcome(string? fullName)
    {
        var name = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(fullName) ? "there" : fullName!);
        var body =
            $"<p>Hi {name},</p>" +
            "<p>Welcome to the HypeGrid list. You&rsquo;ll now get campaign ideas, creator news, and launch tips.</p>" +
            "<p>Regards,<br/>The HypeGrid Team</p>";
        var text = $"Hi {fullName},\n\nWelcome to the HypeGrid list.\n\nRegards,\nThe HypeGrid Team";
        return ("Welcome to HypeGrid", Wrap("Welcome to HypeGrid", body), text);
    }

    // ── Admin / support notifications ───────────────────────────────────────

    public (string Subject, string Html, string Text) EnquiryAdminNotification(Enquiry e)
    {
        var rows = new StringBuilder();
        Row(rows, "Reference", e.Id.ToString());
        Row(rows, "Name", e.FullName);
        Row(rows, "Email", e.Email);
        Row(rows, "Phone", e.Phone);
        Row(rows, "Subject", e.Subject);
        Row(rows, "Interest", e.InterestType);
        Row(rows, "Source page", e.SourcePage);
        Row(rows, "Message", e.Message);
        var body = $"<p>A new general enquiry was submitted.</p>{Table(rows)}{AdminLink("enquiries", e.Id)}";
        return ($"[HypeGrid] New enquiry from {Trim(e.FullName)}", Wrap("New enquiry", body), Strip(rows));
    }

    public (string Subject, string Html, string Text) CampaignRequestAdminNotification(CampaignRequest r)
    {
        var rows = new StringBuilder();
        Row(rows, "Reference", r.Id.ToString());
        Row(rows, "Name", r.FullName);
        Row(rows, "Brand / artist", r.BrandName);
        Row(rows, "Email", r.Email);
        Row(rows, "Phone", r.Phone);
        Row(rows, "What to promote", r.WhatToPromote);
        Row(rows, "Campaign type", r.CampaignType);
        Row(rows, "Target audience", r.TargetAudience);
        Row(rows, "Platforms", r.Platforms.Count > 0 ? string.Join(", ", r.Platforms) : null);
        Row(rows, "Budget", r.BudgetRange);
        Row(rows, "Goal", r.CampaignGoal);
        Row(rows, "Message", r.Message);
        var body = $"<p>A new campaign request was submitted.</p>{Table(rows)}{AdminLink("requests", r.Id)}";
        return ($"[HypeGrid] New campaign request from {Trim(r.FullName)}", Wrap("New campaign request", body), Strip(rows));
    }

    public (string Subject, string Html, string Text) CreatorApplicationAdminNotification(CreatorApplication a)
    {
        var rows = new StringBuilder();
        Row(rows, "Reference", a.Id.ToString());
        Row(rows, "Name", a.FullName);
        Row(rows, "Email", a.Email);
        Row(rows, "Phone", a.Phone);
        Row(rows, "City / Province", string.Join(" ", new[] { a.City, a.Province }.Where(x => !string.IsNullOrWhiteSpace(x))));
        Row(rows, "Main platform", a.MainPlatform);
        Row(rows, "Handle / link", a.HandleOrProfileLink);
        Row(rows, "Followers", a.FollowerRange);
        Row(rows, "Niche", a.ContentNiche);
        Row(rows, "Why HypeGrid", a.ApplicationReason);
        var body = $"<p>A new creator application was submitted.</p>{Table(rows)}{AdminLink("creators", a.Id)}";
        return ($"[HypeGrid] New creator application from {Trim(a.FullName)}", Wrap("New creator application", body), Strip(rows));
    }

    // ── Auth ────────────────────────────────────────────────────────────────

    public (string Subject, string Html, string Text) PasswordReset(string firstName, string resetLink)
    {
        var name = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(firstName) ? "there" : firstName);
        var link = WebUtility.HtmlEncode(resetLink);
        var body =
            $"<p>Hi {name},</p>" +
            "<p>We received a request to reset your HypeGrid admin password. " +
            "Click the button below to choose a new password. If you didn&rsquo;t request this, you can ignore this email.</p>" +
            $"<p style=\"margin:28px 0\"><a href=\"{link}\" " +
            $"style=\"background:{_settings.PrimaryColor};color:#ffffff;padding:12px 22px;border-radius:8px;" +
            "text-decoration:none;font-weight:600;display:inline-block\">Reset password</a></p>" +
            $"<p style=\"font-size:12px;color:#64748b\">Or paste this link into your browser:<br/>{link}</p>";
        var text = $"Hi {firstName},\n\nReset your HypeGrid admin password:\n{resetLink}\n\nIf you didn't request this, ignore this email.";
        return ("Reset your HypeGrid password", Wrap("Reset your password", body), text);
    }

    // ── Shared chrome ───────────────────────────────────────────────────────

    private string Wrap(string heading, string innerHtml)
    {
        var safeHeading = WebUtility.HtmlEncode(heading);
        return
            "<!DOCTYPE html><html><body style=\"margin:0;padding:0;background:#0f172a;font-family:Segoe UI,Arial,sans-serif\">" +
            "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#0f172a;padding:24px 0\">" +
            "<tr><td align=\"center\">" +
            "<table role=\"presentation\" width=\"640\" cellpadding=\"0\" cellspacing=\"0\" " +
            "style=\"max-width:640px;width:100%;background:#ffffff;border-radius:12px;overflow:hidden\">" +
            $"<tr><td style=\"background:{_settings.PrimaryColor};padding:20px 28px;color:#ffffff;font-size:20px;font-weight:700\">HypeGrid</td></tr>" +
            $"<tr><td style=\"padding:28px;color:#0f172a;font-size:15px;line-height:1.6\">" +
            $"<h2 style=\"margin:0 0 16px;font-size:18px\">{safeHeading}</h2>{innerHtml}</td></tr>" +
            $"<tr><td style=\"padding:18px 28px;background:#f1f5f9;color:#64748b;font-size:12px\">" +
            $"{WebUtility.HtmlEncode(_settings.CompanyLegalName)} &middot; {WebUtility.HtmlEncode(_settings.WebsiteUrl)}</td></tr>" +
            "</table></td></tr></table></body></html>";
    }

    private static string Table(StringBuilder rows) =>
        $"<table role=\"presentation\" cellpadding=\"6\" cellspacing=\"0\" style=\"width:100%;border-collapse:collapse\">{rows}</table>";

    private static void Row(StringBuilder sb, string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        sb.Append("<tr>")
          .Append($"<td style=\"vertical-align:top;color:#64748b;font-size:13px;width:160px\">{WebUtility.HtmlEncode(label)}</td>")
          .Append($"<td style=\"color:#0f172a;font-size:14px\">{WebUtility.HtmlEncode(value)}</td>")
          .Append("</tr>");
    }

    private string AdminLink(string section, Guid id) =>
        $"<p style=\"margin-top:20px\"><a href=\"{WebUtility.HtmlEncode($"{_settings.AdminBaseUrl}/{section}")}\" " +
        $"style=\"color:{_settings.PrimaryColor}\">Open in admin portal</a> &middot; ref {id}</p>";

    private static string Trim(string? s) => string.IsNullOrWhiteSpace(s) ? "(no name)" : s.Trim();

    private static string Strip(StringBuilder rows) =>
        WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Replace(rows.ToString(), "<[^>]+>", " ")).Trim();
}
