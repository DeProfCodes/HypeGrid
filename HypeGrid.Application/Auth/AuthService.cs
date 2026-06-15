using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using HypeGrid.Application.Auth.Dtos;
using HypeGrid.Application.Communication.Email.Interfaces;
using HypeGrid.Application.Persistence.Identity;
using HypeGrid.Domain.Identity;
using HypeGrid.Shared.Enums;
using HypeGrid.Shared.Errors;
using HypeGrid.Shared.Results;

namespace HypeGrid.Application.Auth;

/// <summary>
/// Default authentication service over ASP.NET Core Identity + the JWT/refresh
/// token generator. Returns the structured <see cref="Result"/> envelope so the
/// API can map error codes to HTTP status codes centrally.
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly IJwtTokenGenerator _tokens;
    private readonly IEmailService _email;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<User> userManager,
        IJwtTokenGenerator tokens,
        IEmailService email,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _tokens = tokens;
        _email = email;
        _logger = logger;
    }

    public async Task<Result<AuthTokenDto>> LoginAsync(LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return Result<AuthTokenDto>.Failure(ErrorCodes.BadRequest, "Email and password are required.");

        var user = await _userManager.FindByEmailAsync(dto.Email.Trim());
        if (user is null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            return Result<AuthTokenDto>.Failure(ErrorCodes.InvalidCredentials, "Invalid email or password.");

        if (!user.IsActive || user.AccountStatus != AccountStatus.Active)
            return Result<AuthTokenDto>.Failure(ErrorCodes.InactiveAccount, "This account is not active.");

        var tokens = await _tokens.GenerateTokenAsync(user);
        return Result<AuthTokenDto>.Success(tokens, "Logged in.");
    }

    public async Task<Result<Guid>> RegisterAsync(RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return Result<Guid>.Failure(ErrorCodes.BadRequest, "Email and password are required.");

        var existing = await _userManager.FindByEmailAsync(dto.Email.Trim());
        if (existing is not null)
            return Result<Guid>.Failure(ErrorCodes.EmailTaken, "An account with this email already exists.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = dto.Email.Trim(),
            Email = dto.Email.Trim(),
            FirstName = dto.FirstName ?? string.Empty,
            LastName = dto.LastName ?? string.Empty,
            IsActive = true,
            AccountStatus = AccountStatus.Active,
            CreatedOnUtc = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var message = string.Join(" ", result.Errors.Select(e => e.Description));
            var code = result.Errors.Any(e => e.Code.Contains("Password")) ? ErrorCodes.WeakPassword : ErrorCodes.BadRequest;
            return Result<Guid>.Failure(code, message);
        }

        _logger.LogInformation("Registered new HypeGrid user {Email}", user.Email);
        return Result<Guid>.Success(user.Id, "Account created.");
    }

    public async Task<Result<AuthTokenDto>> RefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Result<AuthTokenDto>.Failure(ErrorCodes.BadRequest, "Refresh token is required.");

        var tokens = await _tokens.RefreshTokenAsync(refreshToken);
        return tokens is null
            ? Result<AuthTokenDto>.Failure(ErrorCodes.InvalidRefreshToken, "Refresh token is invalid or expired.")
            : Result<AuthTokenDto>.Success(tokens, "Token refreshed.");
    }

    public async Task<Result> LogoutAsync(string refreshToken)
    {
        if (!string.IsNullOrWhiteSpace(refreshToken))
            await _tokens.RevokeRefreshTokenAsync(refreshToken);
        return Result.Success("Logged out.");
    }

    public async Task<Result<CurrentUserDto>> GetCurrentUserAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result<CurrentUserDto>.Failure(ErrorCodes.NotFound, "User not found.");

        var roles = await _userManager.GetRolesAsync(user);
        return Result<CurrentUserDto>.Success(new CurrentUserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = $"{user.FirstName} {user.LastName}".Trim(),
            PhoneNumber = user.PhoneNumber,
            EmailConfirmed = user.EmailConfirmed,
            Roles = roles.ToList(),
            AccountStatus = user.AccountStatus.ToString()
        });
    }

    public async Task<Result> RequestPasswordResetAsync(ForgotPasswordRequestDto dto, string portalResetUrl)
    {
        // Always succeed (enumeration-safe). Only send mail when the user exists.
        var user = string.IsNullOrWhiteSpace(dto.Email) ? null : await _userManager.FindByEmailAsync(dto.Email.Trim());
        if (user is not null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encoded = Uri.EscapeDataString(token);
            var link = $"{portalResetUrl.TrimEnd('/')}?email={Uri.EscapeDataString(user.Email!)}&token={encoded}";
            await _email.SendPasswordResetAsync(user.Email!, user.FirstName, link);
        }

        return Result.Success("If an account exists with that email, a reset link has been sent.");
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Token) || string.IsNullOrWhiteSpace(dto.NewPassword))
            return Result.Failure(ErrorCodes.BadRequest, "Email, token, and new password are required.");

        var user = await _userManager.FindByEmailAsync(dto.Email.Trim());
        if (user is null)
            return Result.Failure(ErrorCodes.InvalidResetToken, "Invalid or expired reset link.");

        var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
        if (!result.Succeeded)
        {
            var message = string.Join(" ", result.Errors.Select(e => e.Description));
            var code = result.Errors.Any(e => e.Code.Contains("Password")) ? ErrorCodes.WeakPassword : ErrorCodes.InvalidResetToken;
            return Result.Failure(code, message);
        }

        return Result.Success("Your password has been reset.");
    }
}
