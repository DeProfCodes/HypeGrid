namespace HypeGrid.Infrastructure.Configuration;

/// <summary>
/// Bound from the <c>AssetStorage</c> config section. Secrets (R2 keys) come from
/// env vars / app-pool settings and are never committed. See
/// docs/CONFIGURATION.md for the full setup.
/// </summary>
public sealed class AssetStorageSettings
{
    public const string SectionName = "AssetStorage";

    /// <summary>"R2" (Cloudflare, default) or "Local" (dev disk).</summary>
    public string Provider { get; set; } = "R2";

    /// <summary>Public asset base, e.g. https://assets.hypegrid.co.za (no trailing slash needed).</summary>
    public string? PublicBaseUrl { get; set; }

    public string BucketName { get; set; } = "hypegrid-assets";

    public R2Options R2 { get; set; } = new();

    public sealed class R2Options
    {
        public string? AccountId { get; set; }

        /// <summary>S3 endpoint; defaults to https://{AccountId}.r2.cloudflarestorage.com when blank.</summary>
        public string? Endpoint { get; set; }

        public string? AccessKeyId { get; set; }
        public string? SecretAccessKey { get; set; }

        public string ResolveEndpoint() =>
            string.IsNullOrWhiteSpace(Endpoint) && !string.IsNullOrWhiteSpace(AccountId)
                ? $"https://{AccountId}.r2.cloudflarestorage.com"
                : Endpoint ?? string.Empty;
    }
}
