using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HypeGrid.Application.Analytics;
using HypeGrid.Shared.Constants;

namespace HypeGrid.API.Controllers.Admin;

/// <summary>
/// Admin marketing analytics — backs the "Placement Analytics" page. Phase-1
/// basic counts (impressions / clicks / CTR) per placement and per deal, plus a
/// recent-events feed. Admin-only.
/// </summary>
[Authorize(Policy = HypeGridPolicies.RequireAdminAccess)]
[Route("api/admin/analytics")]
public sealed class PlacementAnalyticsController : BaseController
{
    private readonly IAnalyticsService _analytics;

    public PlacementAnalyticsController(IAnalyticsService analytics) => _analytics = analytics;

    [HttpGet("placements")]
    public async Task<IActionResult> Placements(CancellationToken ct)
        => Data(await _analytics.GetPlacementOverviewAsync(ct));

    [HttpGet("deals")]
    public async Task<IActionResult> Deals(CancellationToken ct)
        => Data(await _analytics.GetDealOverviewAsync(ct));

    [HttpGet("recent-events")]
    public async Task<IActionResult> RecentEvents([FromQuery] int limit = 50, CancellationToken ct = default)
        => Data(await _analytics.GetRecentEventsAsync(limit, ct));
}
