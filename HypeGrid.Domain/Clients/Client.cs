using HypeGrid.Domain.Common;

namespace HypeGrid.Domain.Clients;

/// <summary>
/// A HypeGrid client/brand. Mirrors the Base44 <c>Client</c> entity used by the
/// admin Clients + ClientDetail pages. A <see cref="Leads.CampaignRequest"/>
/// can be converted into a Client.
/// </summary>
public class Client : BaseEntity
{
    public string BrandName { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? ClientType { get; set; }
    public string? Industry { get; set; }
    public string Status { get; set; } = "Lead";
    public string? Website { get; set; }
    public string? SocialLinks { get; set; }
    public string? Location { get; set; }

    /// <summary>Denormalised rollups maintained for the admin list view.</summary>
    public decimal TotalSpend { get; set; }
    public int ActiveCampaigns { get; set; }

    public string? Notes { get; set; }
}
