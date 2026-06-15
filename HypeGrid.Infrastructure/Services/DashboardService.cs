using Microsoft.EntityFrameworkCore;
using HypeGrid.Application.Dashboard;
using HypeGrid.Application.Dashboard.Dtos;
using HypeGrid.Domain.Campaigns;
using HypeGrid.Domain.Leads;
using HypeGrid.Infrastructure.Data;

namespace HypeGrid.Infrastructure.Services;

/// <summary>
/// Computes the admin dashboard widgets server-side, applying the same status
/// groupings the frontend (HypeGridAdmin/src/pages/Dashboard.jsx) currently
/// computes client-side from full entity lists.
/// </summary>
public sealed class DashboardService : IDashboardService
{
    private static readonly string[] ActiveCampaignStatuses =
        { "Active", "In Planning", "Content Pending", "Client Review" };
    private static readonly string[] PendingRequestStatuses = { "New", "Contacted" };
    private static readonly string[] PendingDeliverableStatuses = { "Not Started", "In Progress", "Needs Changes" };
    private static readonly string[] PendingCreatorStatuses = { "Applied", "Under Review" };

    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db) => _db = db;

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default)
    {
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        return new DashboardSummaryDto
        {
            ActiveCampaigns = await _db.Campaigns.CountAsync(c => ActiveCampaignStatuses.Contains(c.Status), ct),
            PendingRequests = await _db.CampaignRequests.CountAsync(r => PendingRequestStatuses.Contains(r.Status), ct),
            ActiveClients = await _db.Clients.CountAsync(c => c.Status == "Active", ct),
            CreatorNetwork = await _db.Creators.CountAsync(ct),
            PendingDeliverables = await _db.Deliverables.CountAsync(d => PendingDeliverableStatuses.Contains(d.Status), ct),
            MonthlyRevenue = await _db.Invoices
                .Where(i => i.Status == "Paid" && i.CreatedDate >= monthStart)
                .SumAsync(i => (decimal?)i.PaidAmount, ct) ?? 0m,
            PayoutsDue = await _db.Payouts.CountAsync(p => p.Status == "Pending", ct),
            AwaitingApproval = await _db.Campaigns.CountAsync(c => c.Status == "Awaiting Payment", ct),
            TotalEnquiries = await _db.Enquiries.CountAsync(ct),
            NewsletterSubscribers = await _db.NewsletterSubscribers.CountAsync(s => s.IsActive, ct),
            PendingCreatorApplications = await _db.CreatorApplications.CountAsync(a => PendingCreatorStatuses.Contains(a.Status), ct)
        };
    }

    public async Task<IReadOnlyList<CampaignRequest>> GetRecentCampaignRequestsAsync(int take = 5, CancellationToken ct = default)
        => await _db.CampaignRequests.AsNoTracking().OrderByDescending(x => x.CreatedDate).Take(take).ToListAsync(ct);

    public async Task<IReadOnlyList<Enquiry>> GetRecentEnquiriesAsync(int take = 5, CancellationToken ct = default)
        => await _db.Enquiries.AsNoTracking().OrderByDescending(x => x.CreatedDate).Take(take).ToListAsync(ct);

    public async Task<IReadOnlyList<CreatorApplication>> GetRecentCreatorApplicationsAsync(int take = 5, CancellationToken ct = default)
        => await _db.CreatorApplications.AsNoTracking().OrderByDescending(x => x.CreatedDate).Take(take).ToListAsync(ct);

    public async Task<IReadOnlyList<Campaign>> GetActiveCampaignsAsync(int take = 5, CancellationToken ct = default)
        => await _db.Campaigns.AsNoTracking()
            .Where(c => ActiveCampaignStatuses.Contains(c.Status))
            .OrderByDescending(x => x.CreatedDate).Take(take).ToListAsync(ct);

    public async Task<IReadOnlyList<Deliverable>> GetPendingDeliverablesAsync(int take = 5, CancellationToken ct = default)
        => await _db.Deliverables.AsNoTracking()
            .Where(d => PendingDeliverableStatuses.Contains(d.Status))
            .OrderByDescending(x => x.CreatedDate).Take(take).ToListAsync(ct);

    public async Task<IReadOnlyList<CountByLabelDto>> GetCampaignsByStatusAsync(CancellationToken ct = default)
        => await _db.Campaigns.AsNoTracking()
            .GroupBy(c => c.Status)
            .Select(g => new CountByLabelDto(g.Key, g.Count()))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<CountByLabelDto>> GetCampaignsByTypeAsync(CancellationToken ct = default)
        => await _db.Campaigns.AsNoTracking()
            .Where(c => c.Type != null)
            .GroupBy(c => c.Type!)
            .Select(g => new CountByLabelDto(g.Key, g.Count()))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<CountByLabelDto>> GetLeadsByTypeAsync(CancellationToken ct = default)
        => await _db.CampaignRequests.AsNoTracking()
            .Where(r => r.CampaignType != null)
            .GroupBy(r => r.CampaignType!)
            .Select(g => new CountByLabelDto(g.Key, g.Count()))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<MonthlyCountDto>> GetMonthlyLeadsAsync(int months = 6, CancellationToken ct = default)
    {
        var since = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddMonths(-(months - 1));

        // Combine campaign requests + enquiries as "leads", bucketed by month.
        var requestDates = await _db.CampaignRequests.AsNoTracking()
            .Where(r => r.CreatedDate >= since).Select(r => r.CreatedDate).ToListAsync(ct);
        var enquiryDates = await _db.Enquiries.AsNoTracking()
            .Where(e => e.CreatedDate >= since).Select(e => e.CreatedDate).ToListAsync(ct);

        var all = requestDates.Concat(enquiryDates);

        var buckets = Enumerable.Range(0, months)
            .Select(i => since.AddMonths(i))
            .ToDictionary(d => $"{d:yyyy-MM}", _ => 0);

        foreach (var date in all)
        {
            var key = $"{date:yyyy-MM}";
            if (buckets.ContainsKey(key))
                buckets[key]++;
        }

        return buckets.Select(kv => new MonthlyCountDto { Month = kv.Key, Count = kv.Value })
            .OrderBy(x => x.Month)
            .ToList();
    }
}
