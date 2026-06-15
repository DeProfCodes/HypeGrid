using HypeGrid.Domain.Common;

namespace HypeGrid.Domain.Campaigns;

/// <summary>
/// A managed campaign/project. Mirrors the Base44 <c>Campaign</c> entity used by
/// the admin Campaigns + CampaignDetail pages. Related records (tasks,
/// deliverables, notes, invoices) reference a campaign by <see cref="Id"/> and
/// carry a denormalised <c>campaign_name</c> for display, matching the frontend.
/// </summary>
public class Campaign : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public Guid? ClientId { get; set; }
    public string? ClientName { get; set; }

    public string? Type { get; set; }
    public string Status { get; set; } = "Draft";

    public decimal? Budget { get; set; }
    public int Progress { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public string? Manager { get; set; }
    public string? Objective { get; set; }
    public string? TargetAudience { get; set; }

    /// <summary>Selected platforms. Stored as a JSON array.</summary>
    public List<string> Platforms { get; set; } = new();

    public string? Brief { get; set; }
    public string? Notes { get; set; }

    public string Phase { get; set; } = "Request Received";
}
