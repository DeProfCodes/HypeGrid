namespace HypeGrid.Application.Common.Interfaces.Shared;

/// <summary>
/// Exposes the currently authenticated user (resolved from the JWT) to the
/// application layer. Implemented in the API layer over IHttpContextAccessor.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsInRole(string role);
}
