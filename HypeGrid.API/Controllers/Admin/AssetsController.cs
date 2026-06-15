using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HypeGrid.Application.Storage;
using HypeGrid.Application.Storage.Dtos;
using HypeGrid.Shared.Constants;
using HypeGrid.Shared.Errors;

namespace HypeGrid.API.Controllers.Admin;

/// <summary>
/// Admin image-upload endpoint backing the marketing forms. Stores to the
/// configured object store (Cloudflare R2) and returns the public URL the form
/// saves into the relevant *_image_url field. Image binaries never touch SQL.
/// </summary>
[Authorize(Policy = HypeGridPolicies.RequireAdminAccess)]
[Route("api/admin/assets")]
public sealed class AssetsController : BaseController
{
    private readonly IAssetStorageService _storage;

    public AssetsController(IAssetStorageService storage) => _storage = storage;

    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)] // hard cap above the largest per-category limit (8 MB hero)
    public async Task<IActionResult> Upload([FromForm] IFormFile? file, [FromForm] string? category, CancellationToken ct)
    {
        var cat = AssetCategory.FromKey(category);
        if (cat is null)
            return Bad($"Unknown category. Allowed: {string.Join(", ", AssetCategory.All.Select(c => c.Key))}.");

        if (file is null || file.Length == 0)
            return Bad("No file was uploaded.");

        if (file.Length > cat.MaxBytes)
            return Bad($"File is too large. Max for {cat.Key} is {cat.MaxBytes / AssetCategory.Mb} MB.");

        // Buffer to a seekable stream so we can sniff the signature then upload.
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct);
        buffer.Position = 0;

        var head = new byte[12];
        var read = await buffer.ReadAsync(head.AsMemory(0, 12), ct);
        buffer.Position = 0;

        // Never trust the client extension/content-type — sniff the magic bytes.
        var sniffed = SniffImageContentType(head, read);
        if (sniffed is null || !AssetCategory.AllowedContentTypes.TryGetValue(sniffed, out var ext))
            return Bad("Only JPEG, PNG, or WEBP images are allowed.");

        var key = BuildObjectKey(cat, file.FileName, ext);

        var result = await _storage.UploadAsync(key, buffer, file.Length, sniffed, ct);
        if (!result.IsSuccess)
            return ToActionResult(result); // e.g. 422 PROVIDER_NOT_CONFIGURED — clear, not a crash

        return Data(new AssetUploadResult
        {
            Url = result.Data!,
            Key = key,
            FileName = SafeBaseName(file.FileName),
            ContentType = sniffed,
            SizeBytes = file.Length,
            RecommendedWidth = cat.RecommendedWidth,
            RecommendedHeight = cat.RecommendedHeight,
        }, "Uploaded.");
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private IActionResult Bad(string message) =>
        StatusCode(StatusCodes.Status400BadRequest, new { success = false, code = ErrorCodes.Validation, message });

    private static string BuildObjectKey(AssetCategory cat, string originalName, string ext)
    {
        var now = DateTime.UtcNow;
        var safe = SafeBaseName(originalName);
        var unique = Guid.NewGuid().ToString("N")[..8];
        return $"{cat.Prefix}{now:yyyy}/{now:MM}/{safe}-{unique}.{ext}";
    }

    /// <summary>
    /// Reduces an arbitrary client filename to a safe slug — strips any path,
    /// lowercases, keeps [a-z0-9-], collapses separators. Defeats path traversal
    /// and weird/dangerous names since the raw name is never used directly.
    /// </summary>
    private static string SafeBaseName(string? fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName ?? string.Empty).ToLowerInvariant();
        var chars = name.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var slug = new string(chars);
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        slug = slug.Trim('-');
        if (slug.Length > 40) slug = slug[..40].Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "asset" : slug;
    }

    private static string? SniffImageContentType(byte[] b, int len)
    {
        if (len >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return "image/jpeg";
        if (len >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47
            && b[4] == 0x0D && b[5] == 0x0A && b[6] == 0x1A && b[7] == 0x0A) return "image/png";
        if (len >= 12 && b[0] == (byte)'R' && b[1] == (byte)'I' && b[2] == (byte)'F' && b[3] == (byte)'F'
            && b[8] == (byte)'W' && b[9] == (byte)'E' && b[10] == (byte)'B' && b[11] == (byte)'P') return "image/webp";
        return null;
    }
}
