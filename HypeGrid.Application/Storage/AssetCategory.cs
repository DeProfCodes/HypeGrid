namespace HypeGrid.Application.Storage;

/// <summary>
/// Catalog of admin upload categories. Each maps to an R2 object-key prefix, a
/// max file size, and the recommended image dimensions surfaced to the admin UI
/// and docs. This is the single source of truth for upload validation/routing.
/// </summary>
public sealed record AssetCategory(
    string Key,
    string Prefix,
    long MaxBytes,
    int RecommendedWidth,
    int RecommendedHeight)
{
    public const long Mb = 1024 * 1024;

    public static readonly AssetCategory HeroDesktop = new("hero-desktop", "hero/desktop/", 8 * Mb, 1920, 1080);
    public static readonly AssetCategory HeroMobile = new("hero-mobile", "hero/mobile/", 8 * Mb, 1080, 1920);
    public static readonly AssetCategory Deal = new("deal", "deals/", 5 * Mb, 1200, 800);
    public static readonly AssetCategory FeaturedVideo = new("featured-video", "featured-video/", 5 * Mb, 1280, 720);
    public static readonly AssetCategory Brand = new("brand", "brand/", 5 * Mb, 1080, 1080);
    public static readonly AssetCategory Campaign = new("campaign", "campaigns/", 5 * Mb, 1200, 800);
    public static readonly AssetCategory Creator = new("creator", "creators/", 5 * Mb, 1080, 1080);

    public static readonly IReadOnlyList<AssetCategory> All =
        new[] { HeroDesktop, HeroMobile, Deal, FeaturedVideo, Brand, Campaign, Creator };

    public static AssetCategory? FromKey(string? key) =>
        All.FirstOrDefault(c => string.Equals(c.Key, key, StringComparison.OrdinalIgnoreCase));

    /// <summary>Allowed upload MIME types → canonical file extension (never trust the client extension).</summary>
    public static readonly IReadOnlyDictionary<string, string> AllowedContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = "jpg",
            ["image/png"] = "png",
            ["image/webp"] = "webp",
        };
}
