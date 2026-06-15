using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using HypeGrid.Application.Analytics.Dtos;
using HypeGrid.Application.Common.Interfaces;
using HypeGrid.Domain.Analytics;
using HypeGrid.Domain.Marketing;
using HypeGrid.Shared.Errors;
using HypeGrid.Shared.Results;

namespace HypeGrid.Application.Analytics;

/// <summary>
/// Default analytics service. Recording is lenient and anonymous; the IP is
/// SHA-256 hashed for rough uniqueness only. Admin summaries are computed with
/// simple counts (phase-1 — no pre-aggregation).
/// </summary>
public sealed class AnalyticsService : IAnalyticsService
{
    private readonly IRepository<AnalyticsEvent> _events;
    private readonly IRepository<HeroPlacement> _heroes;
    private readonly IRepository<Deal> _deals;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(
        IRepository<AnalyticsEvent> events,
        IRepository<HeroPlacement> heroes,
        IRepository<Deal> deals,
        ILogger<AnalyticsService> logger)
    {
        _events = events;
        _heroes = heroes;
        _deals = deals;
        _logger = logger;
    }

    public async Task<Result> RecordAsync(AnalyticsEventInput input, string? userAgent, string? ipAddress, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(input.EventType))
            return Result.Failure(ErrorCodes.Validation, "event_type is required.");

        var entity = new AnalyticsEvent
        {
            EventType = input.EventType.Trim(),
            EntityType = string.IsNullOrWhiteSpace(input.EntityType) ? "Other" : input.EntityType.Trim(),
            EntityId = input.EntityId,
            PagePath = Truncate(input.PagePath, 512),
            Referrer = Truncate(input.Referrer, 512),
            SessionId = Truncate(input.SessionId, 100),
            UserAgent = Truncate(userAgent, 512),
            DeviceType = DeviceTypeFromUserAgent(userAgent),
            IpHash = HashIp(ipAddress),
        };

        await _events.AddAsync(entity, ct);
        await _events.SaveChangesAsync(ct);
        return Result.Success("Recorded.");
    }

    public async Task<AnalyticsOverviewDto> GetPlacementOverviewAsync(CancellationToken ct = default)
    {
        var heroes = await _heroes.ListAsync("priority", 200, null, ct);
        var items = new List<AnalyticsSummaryDto>(heroes.Count);
        foreach (var h in heroes)
            items.Add(await SummaryFor("HeroPlacement", h.Id, h.Title, h.IsActive, ct));
        return Rollup(items);
    }

    public async Task<AnalyticsOverviewDto> GetDealOverviewAsync(CancellationToken ct = default)
    {
        var deals = await _deals.ListAsync("priority", 200, null, ct);
        var items = new List<AnalyticsSummaryDto>(deals.Count);
        foreach (var d in deals)
            items.Add(await SummaryFor("Deal", d.Id, d.Title, d.IsActive, ct));
        return Rollup(items);
    }

    public async Task<AnalyticsSummaryDto> GetEntitySummaryAsync(string entityType, Guid entityId, CancellationToken ct = default)
        => await SummaryFor(entityType, entityId, string.Empty, true, ct);

    public async Task<IReadOnlyList<AnalyticsEventDto>> GetRecentEventsAsync(int limit = 50, CancellationToken ct = default)
    {
        var rows = await _events.ListAsync("-created_date", limit <= 0 ? 50 : limit, null, ct);
        return rows.Select(e => new AnalyticsEventDto
        {
            Id = e.Id,
            EventType = e.EventType,
            EntityType = e.EntityType,
            EntityId = e.EntityId,
            PagePath = e.PagePath,
            DeviceType = e.DeviceType,
            CreatedDate = e.CreatedDate,
        }).ToList();
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task<AnalyticsSummaryDto> SummaryFor(string entityType, Guid id, string title, bool isActive, CancellationToken ct)
    {
        // "impression"/"view"/"card_view"/"video_impression" -> impressions;
        // anything containing "click" -> clicks. Translatable to SQL by EF.
        var impressions = await _events.CountAsync(
            e => e.EntityId == id && (e.EventType.Contains("impression") || e.EventType.Contains("view")), ct);
        var clicks = await _events.CountAsync(
            e => e.EntityId == id && e.EventType.Contains("click"), ct);

        return new AnalyticsSummaryDto
        {
            EntityId = id,
            EntityType = entityType,
            Title = title,
            IsActive = isActive,
            Impressions = impressions,
            Clicks = clicks,
            Ctr = impressions > 0 ? Math.Round((double)clicks / impressions, 4) : 0d,
        };
    }

    private static AnalyticsOverviewDto Rollup(List<AnalyticsSummaryDto> items)
    {
        long imp = items.Sum(i => i.Impressions);
        long clk = items.Sum(i => i.Clicks);
        return new AnalyticsOverviewDto
        {
            TotalImpressions = imp,
            TotalClicks = clk,
            Ctr = imp > 0 ? Math.Round((double)clk / imp, 4) : 0d,
            // Strongest performers first.
            Items = items.OrderByDescending(i => i.Clicks).ThenByDescending(i => i.Impressions).ToList(),
        };
    }

    private static string? DeviceTypeFromUserAgent(string? ua)
    {
        if (string.IsNullOrWhiteSpace(ua)) return null;
        var s = ua.ToLowerInvariant();
        if (s.Contains("ipad") || s.Contains("tablet")) return "tablet";
        if (s.Contains("mobi") || s.Contains("android") || s.Contains("iphone")) return "mobile";
        return "desktop";
    }

    private static string? HashIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return null;
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(ip.Trim()));
        return Convert.ToHexString(bytes);
    }

    private static string? Truncate(string? value, int max)
        => string.IsNullOrEmpty(value) ? value : (value.Length <= max ? value : value[..max]);
}
