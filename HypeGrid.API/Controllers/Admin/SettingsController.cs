using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HypeGrid.Application.Common.Interfaces;
using HypeGrid.Domain.Content;
using HypeGrid.Shared.Constants;

namespace HypeGrid.API.Controllers.Admin;

/// <summary>
/// Site settings — backs the admin Settings page (currently fully hardcoded).
/// Settings are stored one row per key, bucketed by group
/// (company / communication / finance / site). GET/PUT operate on a whole group
/// as a flat key→value map.
/// </summary>
[ApiController]
[Authorize(Policy = HypeGridPolicies.RequireAdminAccess)]
[Route("api/admin/settings")]
public sealed class SettingsController : BaseController
{
    private readonly IRepository<SiteSetting> _repo;

    public SettingsController(IRepository<SiteSetting> repo) => _repo = repo;

    [HttpGet("{group}")]
    public async Task<IActionResult> GetGroup(string group, CancellationToken ct)
    {
        var rows = await _repo.ListAsync("key", null, s => s.Group == group, ct);
        return Data(rows.ToDictionary(s => s.Key, s => s.Value));
    }

    [HttpPut("{group}")]
    public async Task<IActionResult> UpdateGroup(string group, [FromBody] Dictionary<string, string?> values, CancellationToken ct)
    {
        var existing = (await _repo.ListAsync(null, null, s => s.Group == group, ct))
            .ToDictionary(s => s.Key, s => s);

        foreach (var (key, value) in values)
        {
            if (existing.TryGetValue(key, out var row))
            {
                row.Value = value;
                _repo.Update(row);
            }
            else
            {
                await _repo.AddAsync(new SiteSetting { Key = key, Value = value, Group = group }, ct);
            }
        }

        await _repo.SaveChangesAsync(ct);
        var refreshed = await _repo.ListAsync("key", null, s => s.Group == group, ct);
        return Data(refreshed.ToDictionary(s => s.Key, s => s.Value), "Settings saved.");
    }
}
