using Microsoft.Extensions.Logging;
using HypeGrid.Application.Communication.Email.Interfaces;
using HypeGrid.Application.Communication.Email.Models;
using HypeGrid.Application.Communication.Email.Templates;
using HypeGrid.Domain.Alerts;
using HypeGrid.Domain.Leads;
using HypeGrid.Shared.Results;

namespace HypeGrid.Application.Communication.Email.Services;

/// <summary>
/// High-level email orchestrator. Each public-website submission triggers a
/// customer acknowledgement plus an internal admin notification. Sends are
/// best-effort: a failed acknowledgement never fails the originating request
/// (the lead is already persisted) — the failure is logged and surfaced in the
/// returned <see cref="Result"/> for the caller to decide on.
/// </summary>
public sealed class EmailService : IEmailService
{
    private readonly IEmailProvider _provider;
    private readonly HypeGridEmailTemplates _templates;
    private readonly HypeGridEmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IEmailProvider provider,
        HypeGridEmailTemplates templates,
        HypeGridEmailSettings settings,
        ILogger<EmailService> logger)
    {
        _provider = provider;
        _templates = templates;
        _settings = settings;
        _logger = logger;
    }

    public Task<Result> SendEnquiryEmailsAsync(Enquiry enquiry, CancellationToken ct = default)
    {
        var ack = _templates.ContactAcknowledgement(enquiry.FullName);
        var admin = _templates.EnquiryAdminNotification(enquiry);
        return FanOutAsync(enquiry.Email, enquiry.FullName, ack, admin, EmailSender.Support, ct);
    }

    public Task<Result> SendCampaignRequestEmailsAsync(CampaignRequest request, CancellationToken ct = default)
    {
        var ack = _templates.CampaignRequestAcknowledgement(request.FullName);
        var admin = _templates.CampaignRequestAdminNotification(request);
        return FanOutAsync(request.Email, request.FullName, ack, admin, EmailSender.Campaigns, ct);
    }

    public Task<Result> SendCreatorApplicationEmailsAsync(CreatorApplication application, CancellationToken ct = default)
    {
        var ack = _templates.CreatorApplicationAcknowledgement(application.FullName);
        var admin = _templates.CreatorApplicationAdminNotification(application);
        return FanOutAsync(application.Email, application.FullName, ack, admin, EmailSender.Creators, ct);
    }

    public async Task<Result> SendNewsletterWelcomeAsync(NewsletterSubscriber subscriber, CancellationToken ct = default)
    {
        var welcome = _templates.NewsletterWelcome(subscriber.FullName);
        return await SendAsync(subscriber.Email, subscriber.FullName, welcome, EmailSender.NoReply, ct);
    }

    public async Task<Result> SendAlertWelcomeAsync(AlertSubscriber subscriber, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(subscriber.Email))
            return Result.Success("No email on file; acknowledgement skipped.");

        var welcome = _templates.AlertWelcome(subscriber.FullName);
        return await SendAsync(subscriber.Email, subscriber.FullName, welcome, EmailSender.NoReply, ct);
    }

    public Task<Result> SendPasswordResetAsync(string toEmail, string firstName, string resetLink, CancellationToken ct = default)
    {
        var msg = _templates.PasswordReset(firstName, resetLink);
        return SendAsync(toEmail, firstName, msg, EmailSender.NoReply, ct);
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private async Task<Result> FanOutAsync(
        string customerEmail,
        string? customerName,
        (string Subject, string Html, string Text) ack,
        (string Subject, string Html, string Text) admin,
        EmailSender customerSender,
        CancellationToken ct)
    {
        var ackResult = await SendAsync(customerEmail, customerName, ack, customerSender, ct);
        // Internal admin notification — BCC the configured copy recipients (if any).
        // Customer acknowledgements above never carry these addresses.
        var adminResult = await SendAsync(
            _settings.AdminNotificationEmail, _settings.CompanyName, admin, EmailSender.Default, ct,
            bcc: _settings.AdminNotificationBccList);

        if (ackResult.IsSuccess && adminResult.IsSuccess)
            return Result.Success();

        // Don't fail the lead capture on a mail hiccup — log and report soft.
        return Result.Success("Saved. Some notification emails could not be sent.");
    }

    private async Task<Result> SendAsync(
        string toEmail,
        string? toName,
        (string Subject, string Html, string Text) content,
        EmailSender sender,
        CancellationToken ct,
        IReadOnlyList<string>? bcc = null)
    {
        var result = await _provider.SendAsync(new EmailMessage
        {
            ToEmail = toEmail,
            ToName = toName,
            Subject = content.Subject,
            HtmlBody = content.Html,
            PlainTextBody = content.Text,
            Sender = sender,
            Bcc = bcc is { Count: > 0 } ? [.. bcc] : []
        }, ct);

        if (!result.IsSuccess)
            _logger.LogWarning("Email send failed to {Recipient}: {Message}", toEmail, result.Message);

        return result;
    }
}
