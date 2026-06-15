using HypeGrid.Domain.Common;

namespace HypeGrid.Domain.Alerts;

/// <summary>
/// A consent-based HypeGrid Alerts / Deals Club subscriber. Captured from the
/// public opt-in questionnaire: the person actively submits their details +
/// preferences and ticks a required consent box, then is shown a CTA to join
/// the HypeGrid WhatsApp Channel.
///
/// Privacy posture (phase 1):
/// - This is permissioned, consent-based audience building — never scraping.
/// - We store <see cref="ChannelJoinClicked"/> only (whether they tapped the
///   CTA). We do NOT and cannot verify an actual WhatsApp Channel join, so there
///   is deliberately no "JoinedChannel" field.
/// - Consent is captured with the exact text shown (<see cref="ConsentSnapshot"/>)
///   and a version tag (<see cref="ConsentTextVersion"/>) for POPIA proof.
/// - The raw IP is never stored — only a one-way hash (<see cref="ConsentIpHash"/>).
/// - <see cref="DirectMessageOptIn"/> stays false in phase 1; direct WhatsApp
///   messaging (Meta WhatsApp Business API) is a separate, future-only consent.
/// </summary>
public class AlertSubscriber : BaseEntity
{
    public string? FullName { get; set; }
    public string? Email { get; set; }

    /// <summary>WhatsApp/contact number. Normalized toward E.164 (+27…) on capture.</summary>
    public string? PhoneNumber { get; set; }

    public string? City { get; set; }
    public string? Province { get; set; }

    /// <summary>Deal/campaign interest categories (HypeGridValues.DealCategories). JSON column.</summary>
    public List<string> Interests { get; set; } = new();

    /// <summary>How often they'd like alerts (HypeGridValues.AlertFrequencies).</summary>
    public string? FrequencyPreference { get; set; }

    /// <summary>Where the opt-in was submitted from (e.g. "alerts", "home", "deals").</summary>
    public string? SourcePage { get; set; }

    /// <summary>One of HypeGridValues.AlertSubscriberStatuses (New / Active / OptedOut / Archived).</summary>
    public string Status { get; set; } = "New";

    public bool IsActive { get; set; } = true;

    // ── Consent (POPIA) ──────────────────────────────────────────────────────
    public bool ConsentGiven { get; set; }

    /// <summary>Version of the consent copy the user accepted, e.g. "hypegrid-alerts-v1".</summary>
    public string? ConsentTextVersion { get; set; }

    /// <summary>The exact consent text shown to and accepted by the user.</summary>
    public string? ConsentSnapshot { get; set; }

    public DateTime ConsentAt { get; set; } = DateTime.UtcNow;

    /// <summary>One-way hash of the caller IP at consent time — never the raw IP.</summary>
    public string? ConsentIpHash { get; set; }

    public string? ConsentUserAgent { get; set; }

    // ── WhatsApp Channel CTA ─────────────────────────────────────────────────
    /// <summary>True once the subscriber taps the "Join WhatsApp Channel" button. Never implies a verified join.</summary>
    public bool ChannelJoinClicked { get; set; }

    public DateTime? ChannelJoinClickedAt { get; set; }

    /// <summary>
    /// Separate, explicit opt-in to receive 1:1 WhatsApp messages via the Meta
    /// WhatsApp Business API. Phase 1: always false (no direct messaging yet).
    /// </summary>
    public bool DirectMessageOptIn { get; set; }

    public DateTime? UnsubscribedAt { get; set; }
}
