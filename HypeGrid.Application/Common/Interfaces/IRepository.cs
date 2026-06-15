using System.Linq.Expressions;
using HypeGrid.Domain.Common;

namespace HypeGrid.Application.Common.Interfaces;

/// <summary>
/// Generic persistence contract for <see cref="BaseEntity"/> aggregates. Backs
/// the admin CRUD endpoints, whose Base44-style call shape is
/// <c>Entity.list("-created_date", limit)</c> / <c>create</c> / <c>update</c> /
/// <c>delete</c>. Implemented by the EF Core <c>Repository&lt;T&gt;</c>.
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    /// <summary>
    /// Returns entities, optionally filtered, ordered by a field, and capped.
    /// </summary>
    /// <param name="sort">A Base44-style sort token, e.g. "-created_date" (desc) or "name" (asc).</param>
    /// <param name="limit">Max rows to return; null = no cap.</param>
    /// <param name="predicate">Optional filter.</param>
    Task<IReadOnlyList<T>> ListAsync(
        string? sort = null,
        int? limit = null,
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken ct = default);

    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);

    Task AddAsync(T entity, CancellationToken ct = default);

    void Update(T entity);

    void Remove(T entity);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
