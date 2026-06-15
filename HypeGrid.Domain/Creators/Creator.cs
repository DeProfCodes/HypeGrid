using HypeGrid.Domain.Common;

namespace HypeGrid.Domain.Creators;

/// <summary>
/// A creator/influencer in the HypeGrid network. Mirrors the Base44
/// <c>Creator</c> entity used by the admin Creators + CreatorDetail pages.
/// </summary>
public class Creator : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Handle { get; set; }
    public string? Platform { get; set; }
    public string? Niche { get; set; }
    public int? Followers { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string Status { get; set; } = "Applied";
    public string? Bio { get; set; }

    /// <summary>Denormalised rollups maintained for the admin views.</summary>
    public decimal TotalEarned { get; set; }
    public decimal TotalPaid { get; set; }
    public int CampaignsCount { get; set; }
}
