namespace HypeGrid.Application.Storage.Dtos;

/// <summary>
/// Result of a successful admin asset upload. Serialized snake_case, so the admin
/// client reads <c>url</c>, <c>key</c>, <c>file_name</c>, <c>content_type</c>,
/// <c>size_bytes</c>, and the recommended dimensions for the chosen category.
/// </summary>
public sealed class AssetUploadResult
{
    public string Url { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public int RecommendedWidth { get; set; }
    public int RecommendedHeight { get; set; }
}
