using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using HypeGrid.Application.Common.Interfaces;
using HypeGrid.Domain.Campaigns;

namespace HypeGrid.API.Controllers.Admin;

/// <summary>
/// Deliverables — backs the central Deliverables page and the approve /
/// request-changes / mark-posted actions on its rows.
/// </summary>
[Route("api/admin/deliverables")]
public sealed class DeliverablesController : AdminCrudController<Deliverable>
{
    public DeliverablesController(IRepository<Deliverable> repo) : base(repo) { }

    protected override Expression<Func<Deliverable, bool>> BuildSearchPredicate(string q)
        => d => d.Title.Contains(q) || (d.CampaignName != null && d.CampaignName.Contains(q));

    [HttpPatch("{id:guid}/status")]
    public Task<IActionResult> SetStatus(Guid id, [FromBody] StatusPatchDto dto, CancellationToken ct)
        => TransitionAsync(id, dto.Status, ct);

    [HttpPost("{id:guid}/approve")]
    public Task<IActionResult> Approve(Guid id, CancellationToken ct) => TransitionAsync(id, "Approved", ct);

    [HttpPost("{id:guid}/request-changes")]
    public Task<IActionResult> RequestChanges(Guid id, CancellationToken ct) => TransitionAsync(id, "Needs Changes", ct);

    [HttpPost("{id:guid}/mark-posted")]
    public Task<IActionResult> MarkPosted(Guid id, CancellationToken ct) => TransitionAsync(id, "Posted", ct);

    private async Task<IActionResult> TransitionAsync(Guid id, string status, CancellationToken ct)
    {
        var deliverable = await Repo.GetByIdAsync(id, ct);
        if (deliverable is null) return MapNotFound();
        deliverable.Status = status;
        Repo.Update(deliverable);
        await Repo.SaveChangesAsync(ct);
        return Data(deliverable, "Status updated.");
    }
}
