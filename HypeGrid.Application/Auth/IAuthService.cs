using HypeGrid.Application.Auth.Dtos;
using HypeGrid.Shared.Results;

namespace HypeGrid.Application.Auth;

/// <summary>
/// Application-layer authentication operations for the HypeGrid admin portal.
/// </summary>
public interface IAuthService
{
    Task<Result<AuthTokenDto>> LoginAsync(LoginDto dto);

    Task<Result<Guid>> RegisterAsync(RegisterDto dto);

    Task<Result<AuthTokenDto>> RefreshTokenAsync(string refreshToken);

    Task<Result> LogoutAsync(string refreshToken);

    Task<Result<CurrentUserDto>> GetCurrentUserAsync(Guid userId);

    /// <summary>
    /// Starts the forgot-password flow by emailing a reset link to the portal.
    /// Always returns success (enumeration-safe) regardless of whether the
    /// account exists.
    /// </summary>
    Task<Result> RequestPasswordResetAsync(ForgotPasswordRequestDto dto, string portalResetUrl);

    Task<Result> ResetPasswordAsync(ResetPasswordRequestDto dto);
}
