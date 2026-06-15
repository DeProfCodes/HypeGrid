namespace HypeGrid.Shared.Constants;

/// <summary>
/// Canonical role names for the HypeGrid admin portal. Roles are seeded on
/// startup and used by <c>[Authorize(Roles = ...)]</c> policies. Client and
/// Creator roles are provisioned for a future self-service portal even though
/// phase 1 is admin-only.
/// </summary>
public static class HypeGridRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string CampaignManager = "CampaignManager";
    public const string Finance = "Finance";
    public const string Support = "Support";
    public const string Client = "Client";
    public const string Creator = "Creator";

    /// <summary>Roles that grant access to the internal admin dashboard.</summary>
    public static readonly string[] AdminRoles =
    {
        SuperAdmin, Admin, CampaignManager, Finance, Support
    };

    public static readonly string[] All =
    {
        SuperAdmin, Admin, CampaignManager, Finance, Support, Client, Creator
    };
}

/// <summary>
/// Named authorization policies registered in the API.
/// </summary>
public static class HypeGridPolicies
{
    public const string RequireAdminAccess = "RequireAdminAccess";
    public const string RequireSuperAdmin = "RequireSuperAdmin";
    public const string RequireFinance = "RequireFinance";
    public const string RequireCampaignManager = "RequireCampaignManager";
    public const string RequireClient = "RequireClient";
    public const string RequireCreator = "RequireCreator";
}
