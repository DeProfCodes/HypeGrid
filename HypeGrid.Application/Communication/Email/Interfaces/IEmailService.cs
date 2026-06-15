using HypeGrid.Domain.Alerts;
using HypeGrid.Domain.Leads;
using HypeGrid.Shared.Results;

namespace HypeGrid.Application.Communication.Email.Interfaces;

/// <summary>
/// High-level, business-facing email operations for HypeGrid. Each public
/// website submission fans out to (a) an admin/support notification and
/// (b) a customer acknowledgement, per COMMUNICATION_SYSTEM_REPORT.md and the
/// HypeGrid brief.
/// </summary>
public interface IEmailService
{
    Task<Result> SendEnquiryEmailsAsync(Enquiry enquiry, CancellationToken ct = default);

    Task<Result> SendCampaignRequestEmailsAsync(CampaignRequest request, CancellationToken ct = default);

    Task<Result> SendCreatorApplicationEmailsAsync(CreatorApplication application, CancellationToken ct = default);

    Task<Result> SendNewsletterWelcomeAsync(NewsletterSubscriber subscriber, CancellationToken ct = default);

    /// <summary>Best-effort acknowledgement that a HypeGrid Alerts / Deals Club opt-in was received.</summary>
    Task<Result> SendAlertWelcomeAsync(AlertSubscriber subscriber, CancellationToken ct = default);

    /// <summary>Sends a password-reset link to an admin user.</summary>
    Task<Result> SendPasswordResetAsync(string toEmail, string firstName, string resetLink, CancellationToken ct = default);
}
