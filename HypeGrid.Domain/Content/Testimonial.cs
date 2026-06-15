using HypeGrid.Domain.Common;

namespace HypeGrid.Domain.Content;

/// <summary>
/// A client testimonial / social-proof quote for the public website.
/// (No hardcoded testimonials exist on the site yet — this powers the
/// admin content-management screen and a future testimonials section.)
/// </summary>
public class Testimonial : BaseEntity
{
    public string ClientName { get; set; } = string.Empty;
    public string? BrandName { get; set; }
    public string? RoleOrCategory { get; set; }
    public string Quote { get; set; } = string.Empty;
    public int? Rating { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
