using HypeGrid.Domain.Common;

namespace HypeGrid.Domain.Finance;

/// <summary>
/// A quote issued to a client. Mirrors the Base44 <c>Quote</c> entity used by
/// the admin Quotes page. Manual tracking only — no payment gateway in phase 1.
/// </summary>
public class Quote : BaseEntity
{
    public string QuoteNumber { get; set; } = string.Empty;

    public Guid? ClientId { get; set; }
    public string? ClientName { get; set; }

    public string? CampaignType { get; set; }
    public string? PackageName { get; set; }

    public decimal Amount { get; set; }
    public string Status { get; set; } = "Draft";

    public List<LineItem> LineItems { get; set; } = new();

    public string? Notes { get; set; }
    public string? Terms { get; set; }
}
