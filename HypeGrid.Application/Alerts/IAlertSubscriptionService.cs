using HypeGrid.Application.Alerts.Dtos;
using HypeGrid.Shared.Results;

namespace HypeGrid.Application.Alerts;

/// <summary>
/// HypeGrid Alerts / Deals Club — consent-based audience opt-in. Persist-first,
/// best-effort side-effects (acknowledgement email): a mail outage never loses a
/// subscriber. No WhatsApp messages are sent in phase 1; the WhatsApp Channel is
/// joined by the user via a public link, and we record only that they tapped the
/// CTA (never a verified join).
/// </summary>
public interface IAlertSubscriptionService
{
    /// <summary>
    /// Validates and stores an opt-in (consent required; at least one of email/phone).
    /// De-dupes by phone/email — an existing subscriber is updated/reactivated rather
    /// than duplicated. Returns the subscriber id and the WhatsApp Channel URL.
    /// </summary>
    Task<Result<AlertSubscribeResultDto>> SubscribeAsync(
        AlertSubscribeDto dto, string? userAgent, string? ipAddress, CancellationToken ct = default);

    /// <summary>
    /// Idempotently records that the subscriber tapped "Join WhatsApp Channel"
    /// (sets <c>ChannelJoinClicked</c>/<c>ChannelJoinClickedAt</c> on first click).
    /// Returns the channel URL so the caller can open it.
    /// </summary>
    Task<Result<AlertChannelClickResultDto>> MarkChannelClickedAsync(Guid id, CancellationToken ct = default);

    /// <summary>Enumeration-safe opt-out by email and/or phone.</summary>
    Task<Result> UnsubscribeAsync(string? email, string? phone, CancellationToken ct = default);

    /// <summary>Resolves the public WhatsApp Channel URL (site setting, with a safe fallback).</summary>
    Task<string> GetChannelUrlAsync(CancellationToken ct = default);
}
