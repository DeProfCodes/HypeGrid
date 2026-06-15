using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HypeGrid.Application.Common.Interfaces;
using HypeGrid.Domain.Alerts;
using HypeGrid.Shared.Constants;
using HypeGrid.Shared.Errors;

namespace HypeGrid.API.Controllers.Admin;

/// <summary>
/// HypeGrid Alerts / Deals Club subscribers — backs the admin "Alerts" page.
/// Read + light management (status / delete) over the consent-based audience.
/// </summary>
[ApiController]
[Authorize(Policy = HypeGridPolicies.RequireAdminAccess)]
[Route("api/admin/alerts/subscribers")]
public sealed class AlertsAdminController : BaseController
{
    private readonly IRepository<AlertSubscriber> _repo;

    public AlertsAdminController(IRepository<AlertSubscriber> repo) => _repo = repo;

    /// <summary>GET / — list with optional status / clicked / interest / search filters.</summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? status = null,
        [FromQuery] bool? clicked = null,
        [FromQuery] string? interest = null,
        [FromQuery] string? search = null,
        [FromQuery] string? sort = "-created_date",
        [FromQuery] int? limit = 200,
        CancellationToken ct = default)
    {
        var s = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        var i = string.IsNullOrWhiteSpace(interest) ? null : interest.Trim();
        var q = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        Expression<Func<AlertSubscriber, bool>> predicate = x =>
            (s == null || x.Status == s) &&
            (clicked == null || x.ChannelJoinClicked == clicked) &&
            (i == null || x.Interests.Contains(i)) &&
            (q == null
                || (x.FullName != null && x.FullName.Contains(q))
                || (x.Email != null && x.Email.Contains(q))
                || (x.PhoneNumber != null && x.PhoneNumber.Contains(q)));

        return Data(await _repo.ListAsync(sort, limit, predicate, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var sub = await _repo.GetByIdAsync(id, ct);
        return sub is null ? NotFoundEnvelope() : Data(sub);
    }

    /// <summary>PATCH /{id}/status — set Active / OptedOut / Archived / New.</summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] StatusPatchDto dto, CancellationToken ct)
    {
        var sub = await _repo.GetByIdAsync(id, ct);
        if (sub is null) return NotFoundEnvelope();

        var status = (dto.Status ?? string.Empty).Trim();
        if (!HypeGridValues.AlertSubscriberStatuses.Contains(status))
            return StatusCode(StatusCodes.Status400BadRequest,
                new { success = false, code = ErrorCodes.Validation, message = "Unknown status." });

        sub.Status = status;
        var optedOut = status is "OptedOut" or "Archived";
        sub.IsActive = !optedOut;
        sub.UnsubscribedAt = status == "OptedOut" ? DateTime.UtcNow : (status == "Active" ? null : sub.UnsubscribedAt);

        _repo.Update(sub);
        await _repo.SaveChangesAsync(ct);
        return Data(sub, "Updated.");
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var sub = await _repo.GetByIdAsync(id, ct);
        if (sub is null) return NotFoundEnvelope();
        _repo.Remove(sub);
        await _repo.SaveChangesAsync(ct);
        return Ok(new { success = true, message = "Deleted." });
    }

    /// <summary>GET /summary (mapped under api/admin/alerts/summary) — counts + breakdowns.</summary>
    [HttpGet("/api/admin/alerts/summary")]
    public async Task<IActionResult> Summary(CancellationToken ct)
    {
        var all = await _repo.ListAsync("-created_date", null, null, ct);

        var total = all.Count;
        var active = all.Count(x => x.IsActive && x.Status == "Active");
        var optedOut = all.Count(x => x.Status == "OptedOut");
        var clicked = all.Count(x => x.ChannelJoinClicked);

        var byInterest = all
            .SelectMany(x => x.Interests ?? new List<string>())
            .GroupBy(v => v)
            .ToDictionary(g => g.Key, g => g.Count());

        var byProvince = all
            .Where(x => !string.IsNullOrWhiteSpace(x.Province))
            .GroupBy(x => x.Province!)
            .ToDictionary(g => g.Key, g => g.Count());

        var recent = all.Take(8).Select(x => new
        {
            id = x.Id,
            full_name = x.FullName,
            email = x.Email,
            phone = x.PhoneNumber,
            province = x.Province,
            status = x.Status,
            channel_clicked = x.ChannelJoinClicked,
            created_date = x.CreatedDate,
        });

        return Data(new
        {
            total,
            active,
            opted_out = optedOut,
            channel_clicked = clicked,
            click_through_rate = total > 0 ? Math.Round((double)clicked / total, 4) : 0d,
            by_interest = byInterest,
            by_province = byProvince,
            recent_subscribers = recent,
        });
    }

    private IActionResult NotFoundEnvelope()
        => StatusCode(StatusCodes.Status404NotFound,
            new { success = false, code = ErrorCodes.NotFound, message = "Subscriber not found." });
}
