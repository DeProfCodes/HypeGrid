using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HypeGrid.Application.Common.Interfaces;
using HypeGrid.Domain.Leads;
using HypeGrid.Shared.Constants;
using HypeGrid.Shared.Errors;

namespace HypeGrid.API.Controllers.Admin;

/// <summary>Newsletter subscribers — backs the admin Newsletter page.</summary>
[ApiController]
[Authorize(Policy = HypeGridPolicies.RequireAdminAccess)]
[Route("api/admin/newsletter/subscribers")]
public sealed class NewsletterAdminController : BaseController
{
    private readonly IRepository<NewsletterSubscriber> _repo;

    public NewsletterAdminController(IRepository<NewsletterSubscriber> repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? sort = "-created_date", [FromQuery] int? limit = 200, CancellationToken ct = default)
        => Data(await _repo.ListAsync(sort, limit, null, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var sub = await _repo.GetByIdAsync(id, ct);
        return sub is null ? NotFoundEnvelope() : Data(sub);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetActive(Guid id, [FromBody] StatusPatchDto dto, CancellationToken ct)
    {
        var sub = await _repo.GetByIdAsync(id, ct);
        if (sub is null) return NotFoundEnvelope();

        var active = string.Equals(dto.Status, "Active", StringComparison.OrdinalIgnoreCase);
        sub.IsActive = active;
        sub.UnsubscribedAt = active ? null : DateTime.UtcNow;
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

    private IActionResult NotFoundEnvelope()
        => StatusCode(StatusCodes.Status404NotFound, new { success = false, code = ErrorCodes.NotFound, message = "Subscriber not found." });
}
