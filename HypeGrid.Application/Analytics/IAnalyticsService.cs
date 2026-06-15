using HypeGrid.Application.Analytics.Dtos;
using HypeGrid.Shared.Results;

namespace HypeGrid.Application.Analytics;

/// <summary>
/// HypeGrid-owned, privacy-light analytics for marketing placements (separate
/// from Google Analytics). Recording is anonymous and best-effort; the admin
/// read methods return simple counts for phase-1 dashboards.
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Records one event. <paramref name="userAgent"/> and <paramref name="ipAddress"/>
    /// are captured server-side; the IP is hashed, never stored raw.
    /// </summary>
    Task<Result> RecordAsync(AnalyticsEventInput input, string? userAgent, string? ipAddress, CancellationToken ct = default);

    /// <summary>Per-hero-placement impressions/clicks + totals.</summary>
    Task<AnalyticsOverviewDto> GetPlacementOverviewAsync(CancellationToken ct = default);

    /// <summary>Per-deal impressions/clicks + totals.</summary>
    Task<AnalyticsOverviewDto> GetDealOverviewAsync(CancellationToken ct = default);

    /// <summary>Counts for a single entity (backs the per-item analytics endpoints).</summary>
    Task<AnalyticsSummaryDto> GetEntitySummaryAsync(string entityType, Guid entityId, CancellationToken ct = default);

    /// <summary>Most recent events (IP-free), newest first.</summary>
    Task<IReadOnlyList<AnalyticsEventDto>> GetRecentEventsAsync(int limit = 50, CancellationToken ct = default);
}
