using HypeGrid.Domain.Common;

namespace HypeGrid.Domain.Marketing;

/// <summary>
/// A featured YouTube/campaign video for the homepage. Modelled as a table (not
/// a singleton) so HypeGrid can rotate/schedule multiple over time; the public
/// site shows the single active + in-date video with the lowest
/// <see cref="SortOrder"/>.
/// </summary>
public class FeaturedVideo : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }

    /// <summary>Full YouTube watch/share URL; the client extracts the embed id.</summary>
    public string YouTubeUrl { get; set; } = string.Empty;

    public string? ThumbnailUrl { get; set; }

    public string? CtaText { get; set; }
    public string? CtaUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    /// <summary>Lower shows first when multiple are active.</summary>
    public int SortOrder { get; set; }
}
