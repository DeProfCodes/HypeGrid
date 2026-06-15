using HypeGrid.Domain.Common;

namespace HypeGrid.Domain.Campaigns;

/// <summary>
/// A free-form note attached to a campaign/client/creator or kept internal.
/// Mirrors the Base44 <c>Note</c> entity used by the admin Messages page and
/// the Campaign detail Notes tab.
/// </summary>
public class Note : BaseEntity
{
    public string Content { get; set; } = string.Empty;

    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? EntityName { get; set; }

    public string? Author { get; set; }
    public string Visibility { get; set; } = "Internal";
}
