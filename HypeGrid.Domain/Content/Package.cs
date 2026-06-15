using HypeGrid.Domain.Common;

namespace HypeGrid.Domain.Content;

/// <summary>
/// A public-website package/plan. Powers content currently hardcoded in
/// HypeGridWebsite/src/pages/Packages.jsx + PackagesPreview.jsx.
/// </summary>
public class Package : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Display price label, e.g. "Request Quote". HypeGrid is quote-based by default.</summary>
    public string? PriceLabel { get; set; }
    public bool IsQuoteBased { get; set; } = true;

    /// <summary>The bullet "includes" list. JSON array.</summary>
    public List<string> Features { get; set; } = new();

    /// <summary>Accent colour key used by the frontend cards (e.g. "cyan", "green").</summary>
    public string? Color { get; set; }
    public string? Cta { get; set; }

    public int SortOrder { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;
}
