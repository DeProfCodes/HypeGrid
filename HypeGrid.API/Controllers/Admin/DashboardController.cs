using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HypeGrid.Application.Dashboard;
using HypeGrid.Shared.Constants;

namespace HypeGrid.API.Controllers.Admin;

/// <summary>
/// Backs the admin Dashboard widgets and recent-activity tables. Each endpoint
/// returns a value the frontend currently computes client-side from full entity
/// lists (HypeGridAdmin/src/pages/Dashboard.jsx + DashboardCharts.jsx).
/// </summary>
[ApiController]
[Authorize(Policy = HypeGridPolicies.RequireAdminAccess)]
[Route("api/admin/dashboard")]
public sealed class DashboardController : BaseController
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard) => _dashboard = dashboard;

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken ct)
        => Data(await _dashboard.GetSummaryAsync(ct));

    [HttpGet("recent-enquiries")]
    public async Task<IActionResult> RecentEnquiries([FromQuery] int take = 5, CancellationToken ct = default)
        => Data(await _dashboard.GetRecentEnquiriesAsync(take, ct));

    [HttpGet("recent-campaign-requests")]
    public async Task<IActionResult> RecentCampaignRequests([FromQuery] int take = 5, CancellationToken ct = default)
        => Data(await _dashboard.GetRecentCampaignRequestsAsync(take, ct));

    [HttpGet("recent-creator-applications")]
    public async Task<IActionResult> RecentCreatorApplications([FromQuery] int take = 5, CancellationToken ct = default)
        => Data(await _dashboard.GetRecentCreatorApplicationsAsync(take, ct));

    [HttpGet("active-campaigns")]
    public async Task<IActionResult> ActiveCampaigns([FromQuery] int take = 5, CancellationToken ct = default)
        => Data(await _dashboard.GetActiveCampaignsAsync(take, ct));

    [HttpGet("pending-deliverables")]
    public async Task<IActionResult> PendingDeliverables([FromQuery] int take = 5, CancellationToken ct = default)
        => Data(await _dashboard.GetPendingDeliverablesAsync(take, ct));

    [HttpGet("campaigns-by-status")]
    public async Task<IActionResult> CampaignsByStatus(CancellationToken ct)
        => Data(await _dashboard.GetCampaignsByStatusAsync(ct));

    [HttpGet("campaigns-by-type")]
    public async Task<IActionResult> CampaignsByType(CancellationToken ct)
        => Data(await _dashboard.GetCampaignsByTypeAsync(ct));

    [HttpGet("leads-by-type")]
    public async Task<IActionResult> LeadsByType(CancellationToken ct)
        => Data(await _dashboard.GetLeadsByTypeAsync(ct));

    [HttpGet("monthly-leads")]
    public async Task<IActionResult> MonthlyLeads([FromQuery] int months = 6, CancellationToken ct = default)
        => Data(await _dashboard.GetMonthlyLeadsAsync(months, ct));
}
