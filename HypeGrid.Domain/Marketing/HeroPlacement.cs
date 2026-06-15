using HypeGrid.Domain.Common;

namespace HypeGrid.Domain.Marketing;

/// <summary>
/// An API-driven homepage hero/carousel slide managed from the admin portal.
/// Sellable advertising inventory: each placement can promote a deal, campaign,
/// sponsor, or internal route. Only active + in-date placements are served to
/// the public site (ordered by <see cref="Priority"/>).
/// </summary>
public class HeroPlacement : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? Badge { get; set; }

    /// <summary>Optional sponsor/brand attribution (e.g. "Sponsored by …").</summary>
    public string? SponsorName { get; set; }

    /// <summary>Recommended 1920x1080.</summary>
    public string? DesktopImageUrl { get; set; }

    /// <summary>Recommended 1080x1920. Falls back to desktop on the client.</summary>
    public string? MobileImageUrl { get; set; }

    public string? CtaText { get; set; }
    public string? CtaUrl { get; set; }

    /// <summary>One of HypeGridValues.CtaTargetTypes: internal | external | whatsapp | deal | campaign.</summary>
    public string CtaTargetType { get; set; } = "internal";

    /// <summary>Optional reference to a related campaign/deal (slug or id).</summary>
    public string? CampaignReference { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    /// <summary>Lower shows first.</summary>
    public int Priority { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>When false, the client should not emit analytics for this placement.</summary>
    public bool TrackingEnabled { get; set; } = true;
}
