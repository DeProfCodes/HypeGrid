using HypeGrid.Domain.Common;

namespace HypeGrid.Domain.Marketing;

/// <summary>
/// A public special / discount / offer published on HypeGrid. Phase 1 is
/// admin-entered only (no scraping). Source attribution fields exist so
/// third-party deals can be shown neutrally ("Special found at …") without
/// implying a partnership unless <see cref="IsSponsored"/> is set. Only active +
/// non-expired deals are served to the public site.
/// </summary>
public class Deal : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    /// <summary>Brand / store the special is from (e.g. "Shoprite").</summary>
    public string? BrandName { get; set; }

    /// <summary>One of HypeGridValues.DealCategories.</summary>
    public string Category { get; set; } = "Other";

    public string? ShortDescription { get; set; }
    public string? FullDescription { get; set; }

    public string? ImageUrl { get; set; }
    public string? MobileImageUrl { get; set; }

    public decimal? OriginalPrice { get; set; }
    public decimal? DealPrice { get; set; }

    /// <summary>Optional display label, e.g. "Save 30%".</summary>
    public string? DiscountLabel { get; set; }

    public string? CtaText { get; set; }
    public string? CtaUrl { get; set; }

    public string? Location { get; set; }
    public string? Province { get; set; }

    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }

    /// <summary>True only for paid placements — gates "partner"/"sponsored" wording.</summary>
    public bool IsSponsored { get; set; }

    /// <summary>Lower shows first.</summary>
    public int Priority { get; set; }

    /// <summary>Where the deal was found (neutral attribution).</summary>
    public string? SourceName { get; set; }
    public string? SourceUrl { get; set; }

    /// <summary>Terms / fine print / notes.</summary>
    public string? Terms { get; set; }
}
