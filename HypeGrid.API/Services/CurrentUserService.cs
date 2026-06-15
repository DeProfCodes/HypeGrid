using System.Security.Claims;
using HypeGrid.Application.Common.Interfaces.Shared;

namespace HypeGrid.API.Services;

/// <summary>
/// Resolves the authenticated user from the current request's JWT claims.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId
    {
        get
        {
            var raw = Principal?.FindFirstValue("user_id")
                      ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email);

    public IReadOnlyList<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? new List<string>();

    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;
}

/// <summary>
/// Bridges ASP.NET hosting environment to the Infrastructure seeder's
/// <see cref="HypeGrid.Infrastructure.Data.Seed.IHostEnvironmentAccessor"/>.
/// </summary>
public sealed class HostEnvironmentAccessor : HypeGrid.Infrastructure.Data.Seed.IHostEnvironmentAccessor
{
    public bool IsDevelopment { get; }

    public HostEnvironmentAccessor(IWebHostEnvironment env) => IsDevelopment = env.IsDevelopment();
}
