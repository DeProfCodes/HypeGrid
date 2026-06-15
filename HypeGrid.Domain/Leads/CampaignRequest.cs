using HypeGrid.Domain.Common;

namespace HypeGrid.Domain.Leads;

/// <summary>
/// A "Start a Campaign" request submitted from the public website
/// (HypeGridWebsite/src/pages/Campaigns.jsx). Mirrors the Base44
/// <c>CampaignRequest</c> entity used by the admin Requests page, plus
/// <see cref="WhatToPromote"/> to capture the website form's "promoteWhat".
/// </summary>
public class CampaignRequest : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string? BrandName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }

    /// <summary>The website form's "What do you want to promote?" field.</summary>
    public string? WhatToPromote { get; set; }

    public string? CampaignType { get; set; }
    public string? TargetAudience { get; set; }

    /// <summary>Selected platforms (TikTok, Instagram, ...). Stored as a JSON array.</summary>
    public List<string> Platforms { get; set; } = new();

    public string? BudgetRange { get; set; }
    public string? CampaignGoal { get; set; }
    public string? Message { get; set; }

    public string Status { get; set; } = "New";
    public Guid? AssignedToUserId { get; set; }
}
