namespace HypeGrid.Domain.Common;

/// <summary>
/// Base class for all HypeGrid persisted entities.
///
/// Note on naming: the timestamp properties are <c>CreatedDate</c> /
/// <c>UpdatedDate</c> (not the <c>CreatedOnUtc</c> convention used elsewhere
/// in the ecosystem) ON PURPOSE — the HypeGrid website/admin frontends were
/// built on Base44, whose entities expose <c>created_date</c> / <c>id</c>, and
/// the admin sorts/filters on those exact field names (e.g.
/// <c>Campaign.list("-created_date", 50)</c>). With the API's snake_case JSON
/// policy these serialize to <c>id</c> / <c>created_date</c> / <c>updated_date</c>,
/// so the existing frontend can bind without renaming a single field.
/// Timestamps are stamped centrally in <c>AppDbContext.SaveChanges</c>.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }
}
