namespace HypeGrid.Application.Common.Validation;

/// <summary>
/// Lightweight helpers for validating user input against the allowed string
/// values in <c>HypeGridValues</c> and for basic email shape checks. Kept
/// dependency-free (no FluentValidation) to match the small surface area; can
/// be swapped for FluentValidation later without touching call sites.
/// </summary>
public static class ValueValidation
{
    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        var at = email.IndexOf('@');
        var dot = email.LastIndexOf('.');
        return at > 0 && dot > at + 1 && dot < email.Length - 1;
    }

    /// <summary>
    /// Returns true when <paramref name="value"/> is null/empty (treated as
    /// "not supplied") or is one of the <paramref name="allowed"/> values.
    /// </summary>
    public static bool IsAllowedOrEmpty(string? value, IEnumerable<string> allowed)
        => string.IsNullOrWhiteSpace(value) || allowed.Contains(value);
}
