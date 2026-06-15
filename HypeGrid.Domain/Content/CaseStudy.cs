using HypeGrid.Domain.Common;

namespace HypeGrid.Domain.Content;

/// <summary>
/// A portfolio / case-study item. Powers the placeholder portfolio cards in
/// HypeGridWebsite/src/components/home/PortfolioPreview.jsx
/// ("Campaign showcase coming soon").
/// </summary>
public class CaseStudy : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Summary { get; set; }
    public string? FullStory { get; set; }
    public string? ImageUrl { get; set; }

    /// <summary>Accent colour key used by the frontend cards.</summary>
    public string? Color { get; set; }

    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
