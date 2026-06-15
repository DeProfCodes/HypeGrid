using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HypeGrid.Application.Storage;
using HypeGrid.Infrastructure.Configuration;
using HypeGrid.Shared.Results;

namespace HypeGrid.Infrastructure.Storage;

/// <summary>
/// Dev-only asset storage that writes to <c>wwwroot/uploads/&lt;key&gt;</c> on disk
/// and serves it via the API's static files. Set <c>AssetStorage:Provider=Local</c>
/// and <c>AssetStorage:PublicBaseUrl</c> to the API origin (e.g.
/// http://localhost:5247) so the returned URLs resolve. NOT for production.
/// </summary>
public sealed class LocalAssetStorageService : IAssetStorageService
{
    private readonly AssetStorageSettings _settings;
    private readonly ILogger<LocalAssetStorageService> _logger;
    private readonly string _root;

    public LocalAssetStorageService(AssetStorageSettings settings, IHostEnvironment env, ILogger<LocalAssetStorageService> logger)
    {
        _settings = settings;
        _logger = logger;
        _root = Path.Combine(env.ContentRootPath, "wwwroot", "uploads");
    }

    public string ProviderName => "Local";

    public bool IsConfigured => true;

    public async Task<Result<string>> UploadAsync(
        string objectKey, Stream content, long sizeBytes, string contentType, CancellationToken ct = default)
    {
        try
        {
            var path = Path.Combine(_root, objectKey.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            await using (var file = File.Create(path))
                await content.CopyToAsync(file, ct);

            var basePart = string.IsNullOrWhiteSpace(_settings.PublicBaseUrl) ? string.Empty : _settings.PublicBaseUrl.TrimEnd('/');
            var url = $"{basePart}/uploads/{objectKey}";
            _logger.LogInformation("Saved asset {Key} ({Size} bytes) to local disk.", objectKey, sizeBytes);
            return Result<string>.Success(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Local asset save failed for {Key}.", objectKey);
            return Result<string>.Failure(HypeGrid.Shared.Errors.ErrorCodes.Exception, "Failed to store the image locally.");
        }
    }
}
