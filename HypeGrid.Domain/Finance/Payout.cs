using HypeGrid.Domain.Common;

namespace HypeGrid.Domain.Finance;

/// <summary>
/// A payout owed to a creator. Mirrors the Base44 <c>Payout</c> entity used by
/// the admin Payouts page. Manual tracking only.
/// </summary>
public class Payout : BaseEntity
{
    public Guid? CreatorId { get; set; }
    public string? CreatorName { get; set; }
    public Guid? CampaignId { get; set; }
    public string? CampaignName { get; set; }

    public string? Deliverable { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending";

    public DateTime? DueDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public string? Notes { get; set; }
}
