namespace HypeGrid.Infrastructure.Configuration;

/// <summary>JWT configuration settings (bound from the "JwtSettings" section).</summary>
public sealed class JwtSettings
{
    public const string SectionName = "JwtSettings";

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; } = 60;
    public int RefreshTokenExpirationDays { get; set; } = 30;
}
