using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HypeGrid.Application.Auth;
using HypeGrid.Application.Auth.Dtos;
using HypeGrid.Application.Common.Interfaces.Shared;
using HypeGrid.Shared.Errors;
using HypeGrid.Shared.Results;

namespace HypeGrid.API.Controllers;

/// <summary>
/// Authentication endpoints for the HypeGrid admin portal (and future
/// client/creator portals). Login is anonymous; <c>/me</c> requires a bearer
/// token.
/// </summary>
[Route("api/auth")]
public sealed class AuthController : BaseController
{
    private readonly IAuthService _auth;
    private readonly ICurrentUserService _currentUser;
    private readonly IConfiguration _config;

    public AuthController(IAuthService auth, ICurrentUserService currentUser, IConfiguration config)
    {
        _auth = auth;
        _currentUser = currentUser;
        _config = config;
    }

    /// <summary>Authenticates a user and returns access + refresh tokens.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
        => ToActionResult(await _auth.LoginAsync(dto));

    /// <summary>Registers a new account (admin-provisioned in phase 1).</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        => ToActionResult(await _auth.RegisterAsync(dto));

    /// <summary>Exchanges a valid refresh token for a fresh token pair.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto)
        => ToActionResult(await _auth.RefreshTokenAsync(dto.RefreshToken));

    /// <summary>Revokes the supplied refresh token.</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto dto)
        => ToActionResult(await _auth.LogoutAsync(dto.RefreshToken));

    /// <summary>Returns the currently authenticated user.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var userId = _currentUser.UserId;
        if (userId is null)
            return ToActionResult(Result<CurrentUserDto>.Failure(ErrorCodes.Unauthorized, "Not authenticated."));

        return ToActionResult(await _auth.GetCurrentUserAsync(userId.Value));
    }

    /// <summary>
    /// Starts the forgot-password flow, emailing a reset link that points at the
    /// admin portal's reset page. Always returns success (enumeration-safe).
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto dto)
    {
        var resetUrl = $"{GetAdminBaseUrl()}/reset-password";
        return ToActionResult(await _auth.RequestPasswordResetAsync(dto, resetUrl));
    }

    /// <summary>Finalises a password reset using the emailed token.</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto dto)
        => ToActionResult(await _auth.ResetPasswordAsync(dto));

    // The reset link must point at the admin portal (which owns the
    // /reset-password route), not the API host.
    private string GetAdminBaseUrl()
        => (_config["HypeGridEmail:AdminBaseUrl"] ?? "https://admin.hypegrid.co.za").TrimEnd('/');
}
