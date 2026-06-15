using HypeGrid.Shared.Results;

namespace HypeGrid.Application.Storage;

/// <summary>
/// Object-storage abstraction for HypeGrid marketing assets. Implemented by the
/// Cloudflare R2 (S3-compatible) provider and a local-disk provider for dev.
/// Binaries are NEVER stored in SQL — only the resulting public URL/key are.
/// </summary>
public interface IAssetStorageService
{
    /// <summary>The provider name (e.g. "R2", "Local") — for diagnostics/logs.</summary>
    string ProviderName { get; }

    /// <summary>
    /// True when the provider has everything it needs to upload (bucket, endpoint,
    /// credentials). When false, <see cref="UploadAsync"/> returns a clear config
    /// error rather than throwing — the API still starts without storage config.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Stores an object at <paramref name="objectKey"/> and returns its public URL.
    /// Validation (type/size/category) is the caller's responsibility.
    /// </summary>
    Task<Result<string>> UploadAsync(
        string objectKey,
        Stream content,
        long sizeBytes,
        string contentType,
        CancellationToken ct = default);
}
