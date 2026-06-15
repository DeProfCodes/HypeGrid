using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HypeGrid.Application.Common.Interfaces.Shared;
using HypeGrid.Domain.Identity;
using HypeGrid.Shared.Constants;
using HypeGrid.Shared.Errors;

namespace HypeGrid.API.Controllers.Admin;

/// <summary>
/// Team / users — backs the admin Team page. Read + role/status management of
/// portal users. Creating users / changing roles requires SuperAdmin.
/// </summary>
[ApiController]
[Authorize(Policy = HypeGridPolicies.RequireAdminAccess)]
[Route("api/admin/users")]
public sealed class TeamController : BaseController
{
    private readonly UserManager<User> _userManager;
    private readonly ICurrentUserService _currentUser;

    public TeamController(UserManager<User> userManager, ICurrentUserService currentUser)
    {
        _userManager = userManager;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var users = await _userManager.Users.OrderBy(u => u.CreatedOnUtc).ToListAsync(ct);
        var result = new List<object>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            result.Add(Project(u, roles));
        }
        return Data(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFoundEnvelope();
        var roles = await _userManager.GetRolesAsync(user);
        return Data(Project(user, roles));
    }

    [HttpPost]
    [Authorize(Policy = HypeGridPolicies.RequireSuperAdmin)]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        if (await _userManager.FindByEmailAsync(dto.Email) is not null)
            return StatusCode(StatusCodes.Status409Conflict, new { success = false, code = ErrorCodes.EmailTaken, message = "Email already in use." });

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName ?? string.Empty,
            LastName = dto.LastName ?? string.Empty,
            EmailConfirmed = true
        };
        var created = await _userManager.CreateAsync(user, dto.Password);
        if (!created.Succeeded)
            return StatusCode(StatusCodes.Status400BadRequest,
                new { success = false, code = ErrorCodes.BadRequest, message = string.Join(" ", created.Errors.Select(e => e.Description)) });

        var role = HypeGridRoles.AdminRoles.Contains(dto.Role) || HypeGridRoles.All.Contains(dto.Role) ? dto.Role : HypeGridRoles.Support;
        await _userManager.AddToRoleAsync(user, role);

        var roles = await _userManager.GetRolesAsync(user);
        return Data(Project(user, roles), "User created.");
    }

    /// <summary>Updates a user's profile fields (name / email). SuperAdmin only.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = HypeGridPolicies.RequireSuperAdmin)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDto dto)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFoundEnvelope();

        if (!string.IsNullOrWhiteSpace(dto.FirstName)) user.FirstName = dto.FirstName!;
        if (!string.IsNullOrWhiteSpace(dto.LastName)) user.LastName = dto.LastName!;
        if (!string.IsNullOrWhiteSpace(dto.Email) && !string.Equals(dto.Email, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            if (await _userManager.FindByEmailAsync(dto.Email!) is not null)
                return StatusCode(StatusCodes.Status409Conflict, new { success = false, code = ErrorCodes.EmailTaken, message = "Email already in use." });
            user.Email = dto.Email;
            user.UserName = dto.Email;
        }
        user.UpdatedOnUtc = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        return Data(Project(user, roles), "User updated.");
    }

    [HttpPatch("{id:guid}/role")]
    [Authorize(Policy = HypeGridPolicies.RequireSuperAdmin)]
    public async Task<IActionResult> SetRole(Guid id, [FromBody] SetRoleDto dto)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFoundEnvelope();
        if (!HypeGridRoles.All.Contains(dto.Role))
            return StatusCode(StatusCodes.Status400BadRequest, new { success = false, code = ErrorCodes.BadRequest, message = "Unknown role." });

        var current = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, current);
        await _userManager.AddToRoleAsync(user, dto.Role);

        var roles = await _userManager.GetRolesAsync(user);
        return Data(Project(user, roles), "Role updated.");
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = HypeGridPolicies.RequireSuperAdmin)]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] StatusPatchDto dto)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFoundEnvelope();
        user.IsActive = string.Equals(dto.Status, "Active", StringComparison.OrdinalIgnoreCase);
        user.UpdatedOnUtc = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        var roles = await _userManager.GetRolesAsync(user);
        return Data(Project(user, roles), "Status updated.");
    }

    /// <summary>
    /// Deletes a portal user. SuperAdmin only, with two guards: you cannot
    /// delete your own account, and you cannot remove the last SuperAdmin
    /// (which would lock everyone out of user management).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = HypeGridPolicies.RequireSuperAdmin)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFoundEnvelope();

        if (_currentUser.UserId == id)
            return StatusCode(StatusCodes.Status400BadRequest,
                new { success = false, code = ErrorCodes.BadRequest, message = "You cannot delete your own account." });

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains(HypeGridRoles.SuperAdmin))
        {
            var superAdmins = await _userManager.GetUsersInRoleAsync(HypeGridRoles.SuperAdmin);
            if (superAdmins.Count <= 1)
                return StatusCode(StatusCodes.Status400BadRequest,
                    new { success = false, code = ErrorCodes.BadRequest, message = "Cannot delete the last SuperAdmin." });
        }

        await _userManager.DeleteAsync(user);
        return Ok(new { success = true, message = "User deleted." });
    }

    private static object Project(User u, IList<string> roles) => new
    {
        id = u.Id,
        email = u.Email,
        full_name = $"{u.FirstName} {u.LastName}".Trim(),
        first_name = u.FirstName,
        last_name = u.LastName,
        role = roles.FirstOrDefault() ?? "",
        roles,
        is_active = u.IsActive,
        created_date = u.CreatedOnUtc
    };

    private IActionResult NotFoundEnvelope()
        => StatusCode(StatusCodes.Status404NotFound, new { success = false, code = ErrorCodes.NotFound, message = "User not found." });
}

public sealed class CreateUserDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string Role { get; set; } = HypeGridRoles.Support;
}

public sealed class UpdateUserDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
}

public sealed class SetRoleDto
{
    public string Role { get; set; } = string.Empty;
}
