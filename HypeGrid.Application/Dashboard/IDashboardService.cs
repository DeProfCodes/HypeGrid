using HypeGrid.Application.Dashboard.Dtos;
using HypeGrid.Domain.Campaigns;
using HypeGrid.Domain.Leads;

namespace HypeGrid.Application.Dashboard;

/// <summary>
/// Server-side aggregations that back the admin dashboard widgets and the
/// recent-activity tables.
/// </summary>
public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default);

    Task<IReadOnlyList<CampaignRequest>> GetRecentCampaignRequestsAsync(int take = 5, CancellationToken ct = default);
    Task<IReadOnlyList<Enquiry>> GetRecentEnquiriesAsync(int take = 5, CancellationToken ct = default);
    Task<IReadOnlyList<CreatorApplication>> GetRecentCreatorApplicationsAsync(int take = 5, CancellationToken ct = default);
    Task<IReadOnlyList<Campaign>> GetActiveCampaignsAsync(int take = 5, CancellationToken ct = default);
    Task<IReadOnlyList<Deliverable>> GetPendingDeliverablesAsync(int take = 5, CancellationToken ct = default);

    Task<IReadOnlyList<CountByLabelDto>> GetCampaignsByStatusAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CountByLabelDto>> GetCampaignsByTypeAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CountByLabelDto>> GetLeadsByTypeAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MonthlyCountDto>> GetMonthlyLeadsAsync(int months = 6, CancellationToken ct = default);
}
