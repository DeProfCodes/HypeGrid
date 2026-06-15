using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Logging;
using HypeGrid.Application.Storage;
using HypeGrid.Infrastructure.Configuration;
using HypeGrid.Shared.Errors;
using HypeGrid.Shared.Results;

namespace HypeGrid.Infrastructure.Storage;

/// <summary>
/// Cloudflare R2 asset storage via the S3-compatible AWS SDK. R2 keys come from
/// config/env and are never committed. If the provider is not fully configured,
/// <see cref="UploadAsync"/> returns a clear config error — it never crashes
/// startup (the S3 client is created lazily on first use).
/// </summary>
public sealed class CloudflareR2AssetStorageService : IAssetStorageService
{
    private readonly AssetStorageSettings _settings;
    private readonly ILogger<CloudflareR2AssetStorageService> _logger;
    private IAmazonS3? _client;

    public CloudflareR2AssetStorageService(AssetStorageSettings settings, ILogger<CloudflareR2AssetStorageService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public string ProviderName => "R2";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_settings.BucketName)
        && !string.IsNullOrWhiteSpace(_settings.PublicBaseUrl)
        && !string.IsNullOrWhiteSpace(_settings.R2.ResolveEndpoint())
        && !string.IsNullOrWhiteSpace(_settings.R2.AccessKeyId)
        && !string.IsNullOrWhiteSpace(_settings.R2.SecretAccessKey);

    public async Task<Result<string>> UploadAsync(
        string objectKey, Stream content, long sizeBytes, string contentType, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("R2 asset storage is not configured; rejecting upload of {Key}.", objectKey);
            return Result<string>.Failure(ErrorCodes.ProviderNotConfigured,
                "Image storage (Cloudflare R2) is not configured on the server. Set the AssetStorage__R2__* env vars.");
        }

        try
        {
            var client = GetClient();
            var put = new PutObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = objectKey,
                InputStream = content,
                ContentType = contentType,
                // R2 doesn't support AWS chunked-payload signing; disable it.
                DisablePayloadSigning = true,
            };
            put.Headers.CacheControl = "public, max-age=31536000, immutable";

            await client.PutObjectAsync(put, ct);

            var url = PublicUrl(objectKey);
            _logger.LogInformation("Uploaded asset {Key} ({Size} bytes) to R2.", objectKey, sizeBytes);
            return Result<string>.Success(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "R2 upload failed for {Key}.", objectKey);
            return Result<string>.Failure(ErrorCodes.Exception, "Failed to store the image. Please try again.");
        }
    }

    private IAmazonS3 GetClient()
    {
        if (_client is not null) return _client;

        var config = new AmazonS3Config
        {
            ServiceURL = _settings.R2.ResolveEndpoint(),
            ForcePathStyle = true,
            // R2 ignores region but the SDK must sign with one.
            AuthenticationRegion = "auto",
        };
        var creds = new BasicAWSCredentials(_settings.R2.AccessKeyId, _settings.R2.SecretAccessKey);
        _client = new AmazonS3Client(creds, config);
        return _client;
    }

    private string PublicUrl(string key) => $"{_settings.PublicBaseUrl!.TrimEnd('/')}/{key}";
}
