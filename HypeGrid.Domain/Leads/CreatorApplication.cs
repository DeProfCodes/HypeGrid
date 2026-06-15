using HypeGrid.Domain.Common;

namespace HypeGrid.Domain.Leads;

/// <summary>
/// A creator-network application submitted from the public website
/// (HypeGridWebsite/src/pages/Creators.jsx). On approval it is converted into
/// a <see cref="Creators.Creator"/>.
/// </summary>
public class CreatorApplication : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }

    public string? MainPlatform { get; set; }

    /// <summary>"@handle" or a profile URL.</summary>
    public string? HandleOrProfileLink { get; set; }

    /// <summary>Follower band (e.g. "1K–5K"), kept as a label.</summary>
    public string? FollowerRange { get; set; }

    public string? ContentNiche { get; set; }

    /// <summary>The website form's "why" textarea.</summary>
    public string? ApplicationReason { get; set; }

    public string Status { get; set; } = "Applied";
    public string? ReviewNotes { get; set; }
}
