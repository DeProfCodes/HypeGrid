using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using HypeGrid.Application.Common.Interfaces;
using HypeGrid.Domain.Common;
using HypeGrid.Infrastructure.Data;

namespace HypeGrid.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IRepository{T}"/>. Translates the
/// Base44-style <c>sort</c> token (e.g. "-created_date") used by the admin
/// frontend into an OrderBy over the matching CLR property.
/// </summary>
public sealed class Repository<T> : IRepository<T> where T : BaseEntity
{
    private readonly AppDbContext _db;
    private readonly DbSet<T> _set;

    public Repository(AppDbContext db)
    {
        _db = db;
        _set = db.Set<T>();
    }

    public async Task<IReadOnlyList<T>> ListAsync(
        string? sort = null,
        int? limit = null,
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken ct = default)
    {
        IQueryable<T> query = _set.AsNoTracking();

        if (predicate is not null)
            query = query.Where(predicate);

        query = ApplySort(query, sort);

        if (limit is > 0)
            query = query.Take(limit.Value);

        return await query.ToListAsync(ct);
    }

    public Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _set.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => _set.FirstOrDefaultAsync(predicate, ct);

    public Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
        => predicate is null ? _set.CountAsync(ct) : _set.CountAsync(predicate, ct);

    public async Task AddAsync(T entity, CancellationToken ct = default)
    {
        if (entity.Id == Guid.Empty)
            entity.Id = Guid.NewGuid();
        await _set.AddAsync(entity, ct);
    }

    public void Update(T entity) => _set.Update(entity);

    public void Remove(T entity) => _set.Remove(entity);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

    /// <summary>
    /// Maps a Base44 sort token to an OrderBy. A leading "-" means descending.
    /// "created_date" / "updated_date" map to the audit columns; anything else
    /// is treated as a CLR property name (PascalCase, case-insensitive). Falls
    /// back to newest-first when the token is missing or unrecognised.
    /// </summary>
    private static IQueryable<T> ApplySort(IQueryable<T> query, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
            return query.OrderByDescending(x => x.CreatedDate);

        var descending = sort.StartsWith('-');
        var field = sort.TrimStart('-', '+').Trim();

        var clrName = field switch
        {
            "created_date" => nameof(BaseEntity.CreatedDate),
            "updated_date" => nameof(BaseEntity.UpdatedDate),
            _ => ToPascalCase(field)
        };

        var prop = typeof(T).GetProperty(clrName,
            System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        if (prop is null)
            return query.OrderByDescending(x => x.CreatedDate);

        // Build x => x.<prop> as an Expression so EF can translate it to SQL.
        var param = Expression.Parameter(typeof(T), "x");
        var body = Expression.Convert(Expression.Property(param, prop), typeof(object));
        var selector = Expression.Lambda<Func<T, object>>(body, param);

        return descending ? query.OrderByDescending(selector) : query.OrderBy(selector);
    }

    private static string ToPascalCase(string snakeOrCamel)
    {
        var parts = snakeOrCamel.Split('_', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Select(p => char.ToUpperInvariant(p[0]) + p[1..]));
    }
}
