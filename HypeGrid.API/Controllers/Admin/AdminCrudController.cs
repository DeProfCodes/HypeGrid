using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HypeGrid.Application.Common.Interfaces;
using HypeGrid.Domain.Common;
using HypeGrid.Shared.Constants;
using HypeGrid.Shared.Errors;

namespace HypeGrid.API.Controllers.Admin;

/// <summary>
/// Generic admin CRUD controller backing the Base44-style entity endpoints the
/// admin frontend calls (<c>Entity.list("-created_date", n)</c> / <c>create</c>
/// / <c>update</c> / <c>delete</c>). Concrete controllers subclass this with a
/// closed generic and a route; entity-specific actions (status patches,
/// conversions) are added on the subclass. Protected by the admin policy.
/// </summary>
[Authorize(Policy = HypeGridPolicies.RequireAdminAccess)]
public abstract class AdminCrudController<T> : BaseController where T : BaseEntity
{
    protected readonly IRepository<T> Repo;

    protected AdminCrudController(IRepository<T> repo) => Repo = repo;

    /// <summary>
    /// Optional per-entity free-text search. Override to support <c>?q=</c>.
    /// </summary>
    protected virtual Expression<Func<T, bool>>? BuildSearchPredicate(string query) => null;

    /// <summary>GET /  — list with Base44 sort token + optional limit/search.</summary>
    [HttpGet]
    public virtual async Task<IActionResult> List(
        [FromQuery] string? sort = "-created_date",
        [FromQuery] int? limit = 100,
        [FromQuery] string? q = null,
        CancellationToken ct = default)
    {
        Expression<Func<T, bool>>? predicate = string.IsNullOrWhiteSpace(q) ? null : BuildSearchPredicate(q!);
        var items = await Repo.ListAsync(sort, limit, predicate, ct);
        return Data(items);
    }

    /// <summary>GET /{id}</summary>
    [HttpGet("{id:guid}")]
    public virtual async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var entity = await Repo.GetByIdAsync(id, ct);
        return entity is null
            ? MapNotFound()
            : Data(entity);
    }

    /// <summary>POST /  — create. Server assigns Id and stamps CreatedDate.</summary>
    [HttpPost]
    public virtual async Task<IActionResult> Create([FromBody] T entity, CancellationToken ct = default)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedDate = default;
        entity.UpdatedDate = null;

        await Repo.AddAsync(entity, ct);
        await Repo.SaveChangesAsync(ct);
        return Data(entity, "Created.");
    }

    /// <summary>PUT /{id}  — full update; Id and CreatedDate are preserved.</summary>
    [HttpPut("{id:guid}")]
    public virtual async Task<IActionResult> Update(Guid id, [FromBody] T incoming, CancellationToken ct = default)
    {
        var existing = await Repo.GetByIdAsync(id, ct);
        if (existing is null) return MapNotFound();

        CopyInto(existing, incoming);
        Repo.Update(existing);
        await Repo.SaveChangesAsync(ct);
        return Data(existing, "Updated.");
    }

    /// <summary>DELETE /{id}</summary>
    [HttpDelete("{id:guid}")]
    public virtual async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var existing = await Repo.GetByIdAsync(id, ct);
        if (existing is null) return MapNotFound();

        Repo.Remove(existing);
        await Repo.SaveChangesAsync(ct);
        return Ok(new { success = true, message = "Deleted." });
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    protected IActionResult MapNotFound() =>
        StatusCode(StatusCodes.Status404NotFound,
            new { success = false, code = ErrorCodes.NotFound, message = $"{typeof(T).Name} not found." });

    /// <summary>
    /// Copies all public writable scalar/collection properties from
    /// <paramref name="incoming"/> onto the tracked <paramref name="target"/>,
    /// skipping identity and audit fields so a PUT can never spoof them.
    /// </summary>
    private static void CopyInto(T target, T incoming)
    {
        foreach (var prop in WritableProps)
            prop.SetValue(target, prop.GetValue(incoming));
    }

    private static readonly PropertyInfo[] WritableProps = typeof(T)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.CanRead && p.CanWrite
                    && p.Name != nameof(BaseEntity.Id)
                    && p.Name != nameof(BaseEntity.CreatedDate)
                    && p.Name != nameof(BaseEntity.UpdatedDate))
        .ToArray();
}
