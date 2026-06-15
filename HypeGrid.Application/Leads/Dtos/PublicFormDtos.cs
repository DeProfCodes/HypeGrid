namespace HypeGrid.Application.Leads.Dtos;

/// <summary>Public Contact form (HypeGridWebsite/src/pages/Contact.jsx).</summary>
public sealed class ContactFormDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Subject { get; set; }
    public string? Interest { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? SourcePage { get; set; }
}

/// <summary>Public "Start a Campaign" form (HypeGridWebsite/src/pages/Campaigns.jsx).</summary>
public sealed class CampaignRequestFormDto
{
    public string FullName { get; set; } = string.Empty;
    public string? BrandName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? PromoteWhat { get; set; }
    public string? CampaignType { get; set; }
    public string? TargetAudience { get; set; }
    public List<string> Platforms { get; set; } = new();
    public string? Budget { get; set; }
    public string? Goal { get; set; }
    public string? Message { get; set; }
}

/// <summary>Public Creator application form (HypeGridWebsite/src/pages/Creators.jsx).</summary>
public sealed class CreatorApplicationFormDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? Platform { get; set; }
    public string? Handle { get; set; }
    public string? Followers { get; set; }
    public string? Niche { get; set; }
    public string? Why { get; set; }
}

/// <summary>Public newsletter subscription.</summary>
public sealed class NewsletterSubscribeDto
{
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? SourcePage { get; set; }
}
