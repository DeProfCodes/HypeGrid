using HypeGrid.Domain.Common;

namespace HypeGrid.Domain.Campaigns;

/// <summary>
/// A content deliverable produced for a campaign (and usually by a creator).
/// Mirrors the Base44 <c>Deliverable</c> entity used by the admin Deliverables
/// page and the Campaign/Creator detail tabs.
/// </summary>
public class Deliverable : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    public Guid? CampaignId { get; set; }
    public string? CampaignName { get; set; }
    public string? ClientName { get; set; }

    public Guid? CreatorId { get; set; }
    public string? CreatorName { get; set; }

    public string? Type { get; set; }
    public string? Platform { get; set; }
    public string Status { get; set; } = "Not Started";

    public DateTime? DueDate { get; set; }
    public string? FileUrl { get; set; }
    public string? Notes { get; set; }
}
