namespace HypeGrid.Application.Auth.Dtos;

/// <summary>Email/password login request.</summary>
public sealed class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>New account registration request.</summary>
public sealed class RegisterDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

/// <summary>Refresh-token exchange request.</summary>
public sealed class RefreshTokenRequestDto
{
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>Forgot-password request — issues a reset link by email.</summary>
public sealed class ForgotPasswordRequestDto
{
    public string Email { get; set; } = string.Empty;
}

/// <summary>Finalises a password reset using the emailed token.</summary>
public sealed class ResetPasswordRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>Issued authentication tokens + the authenticated user.</summary>
public sealed class AuthTokenDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public Guid UserId { get; set; }
    public CurrentUserDto User { get; set; } = new();
}

/// <summary>Current authenticated user projection (also used by /auth/me).</summary>
public sealed class CurrentUserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool EmailConfirmed { get; set; }
    public List<string> Roles { get; set; } = new();
    public string AccountStatus { get; set; } = string.Empty;
}
