namespace HypeGrid.Application.Analytics.Dtos;

/// <summary>
/// Payload posted by the public website to the anonymous analytics endpoint.
/// Personal data must NOT be sent here — only an anonymous session id. The
/// server derives device type from the user agent and stores a one-way IP hash.
/// </summary>
public sealed class AnalyticsEventInput
{
    public string? EventType { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? PagePath { get; set; }
    public string? Referrer { get; set; }
    public string? SessionId { get; set; }
}

/// <summary>Per-entity performance counts for the admin analytics views.</summary>
public sealed class AnalyticsSummaryDto
{
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public long Impressions { get; set; }
    public long Clicks { get; set; }
    /// <summary>Click-through rate 0..1 (clicks / impressions).</summary>
    public double Ctr { get; set; }
}

/// <summary>A summary list plus rolled-up totals for one entity type.</summary>
public sealed class AnalyticsOverviewDto
{
    public long TotalImpressions { get; set; }
    public long TotalClicks { get; set; }
    public double Ctr { get; set; }
    public IReadOnlyList<AnalyticsSummaryDto> Items { get; set; } = new List<AnalyticsSummaryDto>();
}

/// <summary>A slim, IP-free view of a recent event for the admin feed.</summary>
public sealed class AnalyticsEventDto
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? PagePath { get; set; }
    public string? DeviceType { get; set; }
    public DateTime CreatedDate { get; set; }
}
