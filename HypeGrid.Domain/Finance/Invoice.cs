using HypeGrid.Domain.Common;

namespace HypeGrid.Domain.Finance;

/// <summary>
/// An invoice raised against a client/campaign. Mirrors the Base44
/// <c>Invoice</c> entity used by the admin Payments page. Manual tracking only.
/// </summary>
public class Invoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;

    public Guid? ClientId { get; set; }
    public string? ClientName { get; set; }
    public Guid? CampaignId { get; set; }
    public string? CampaignName { get; set; }

    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Outstanding { get; set; }

    public string Status { get; set; } = "Draft";
    public DateTime? DueDate { get; set; }

    public List<LineItem> LineItems { get; set; } = new();

    public string? Notes { get; set; }
}
