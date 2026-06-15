using HypeGrid.Domain.Common;

namespace HypeGrid.Domain.Content;

/// <summary>
/// A public-website service offering. Powers content currently hardcoded in
/// HypeGridWebsite/src/pages/Services.jsx + ServicesPreview.jsx so it can be
/// CMS-managed from the admin.
/// </summary>
public class Service : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? FullDescription { get; set; }

    /// <summary>The bullet "includes" list shown on each service card. JSON array.</summary>
    public List<string> Includes { get; set; } = new();

    /// <summary>Lucide icon name / key used by the frontend.</summary>
    public string? Icon { get; set; }
    public string? Category { get; set; }

    public int SortOrder { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;
}
