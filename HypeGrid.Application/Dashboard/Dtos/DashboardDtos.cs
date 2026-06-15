namespace HypeGrid.Application.Dashboard.Dtos;

/// <summary>
/// Headline stat cards for the admin Dashboard
/// (HypeGridAdmin/src/pages/Dashboard.jsx). Each value is computed server-side
/// from the same rules the frontend currently applies client-side.
/// </summary>
public sealed class DashboardSummaryDto
{
    public int ActiveCampaigns { get; set; }
    public int PendingRequests { get; set; }
    public int ActiveClients { get; set; }
    public int CreatorNetwork { get; set; }
    public int PendingDeliverables { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public int PayoutsDue { get; set; }
    public int AwaitingApproval { get; set; }

    // Extra rollups used by other widgets / the public-facing brief.
    public int TotalEnquiries { get; set; }
    public int NewsletterSubscribers { get; set; }
    public int PendingCreatorApplications { get; set; }
}

/// <summary>A single label/count pair for chart widgets.</summary>
public sealed class CountByLabelDto
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }

    public CountByLabelDto() { }
    public CountByLabelDto(string label, int count)
    {
        Label = label;
        Count = count;
    }
}

/// <summary>A month bucket for the monthly-leads trend widget.</summary>
public sealed class MonthlyCountDto
{
    public string Month { get; set; } = string.Empty; // e.g. "2026-06"
    public int Count { get; set; }
}
