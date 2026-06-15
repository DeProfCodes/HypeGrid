using HypeGrid.Domain.Common;

namespace HypeGrid.Domain.Analytics;

/// <summary>
/// A lightweight, HypeGrid-owned analytics event (separate from Google
/// Analytics) for placement/deal/video performance. Anonymous: no personal data
/// is stored — only a client-supplied session id and a one-way hash of the IP
/// (for rough uniqueness). Written by the anonymous public endpoint.
/// </summary>
public class AnalyticsEvent : BaseEntity
{
    /// <summary>One of HypeGridValues.AnalyticsEventTypes (e.g. "impression", "cta_click").</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>One of HypeGridValues.AnalyticsEntityTypes (HeroPlacement, Deal, FeaturedVideo, …).</summary>
    public string EntityType { get; set; } = string.Empty;

    public Guid? EntityId { get; set; }

    public string? PagePath { get; set; }
    public string? Referrer { get; set; }
    public string? UserAgent { get; set; }

    /// <summary>Coarse device class derived from the user agent: mobile | tablet | desktop.</summary>
    public string? DeviceType { get; set; }

    /// <summary>Anonymous client/session id supplied by the browser (not a user id).</summary>
    public string? SessionId { get; set; }

    /// <summary>One-way hash of the caller IP — never the raw IP.</summary>
    public string? IpHash { get; set; }
}
