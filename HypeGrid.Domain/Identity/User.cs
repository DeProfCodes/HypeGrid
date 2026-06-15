using Microsoft.AspNetCore.Identity;
using HypeGrid.Shared.Enums;

namespace HypeGrid.Domain.Identity;

/// <summary>
/// Represents a HypeGrid portal user (admin team member, and later
/// client/creator portal accounts). Kept close to the database table structure.
/// </summary>
public class User : IdentityUser<Guid>
{
    /// <summary>User first name.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>User last name.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Indicates whether the account is active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Current account lifecycle status.</summary>
    public AccountStatus AccountStatus { get; set; } = AccountStatus.Active;

    /// <summary>UTC timestamp when the user was created.</summary>
    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp when the user was last updated.</summary>
    public DateTime? UpdatedOnUtc { get; set; }
}
