namespace HypeGrid.Shared.Errors;

/// <summary>
/// Common application error codes returned in the <c>Result</c> envelope.
/// Clients may branch on these codes for targeted error UX. The string
/// values are mapped to HTTP status codes in <c>BaseController.MapFailure</c>.
/// </summary>
public static class ErrorCodes
{
    // Generic buckets
    public const string BadRequest = "BAD_REQUEST";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string NotFound = "NOT_FOUND";
    public const string Conflict = "CONFLICT";
    public const string Exception = "EXCEPTION";
    public const string TooManyRequests = "TOO_MANY_REQUESTS";
    public const string Validation = "VALIDATION_ERROR";

    // Auth specifics
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string EmailTaken = "EMAIL_TAKEN";
    public const string WeakPassword = "WEAK_PASSWORD";
    public const string InactiveAccount = "INACTIVE_ACCOUNT";
    public const string EmailNotConfirmed = "EMAIL_NOT_CONFIRMED";
    public const string InvalidRefreshToken = "INVALID_REFRESH_TOKEN";
    public const string RefreshTokenExpired = "REFRESH_TOKEN_EXPIRED";
    public const string InvalidResetToken = "INVALID_RESET_TOKEN";

    // Communications specifics
    public const string EmailSendFailed = "EMAIL_SEND_FAILED";
    public const string ProviderNotConfigured = "PROVIDER_NOT_CONFIGURED";
}
