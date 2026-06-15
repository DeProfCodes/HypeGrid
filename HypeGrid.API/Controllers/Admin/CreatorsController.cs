using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using HypeGrid.Application.Common.Interfaces;
using HypeGrid.Domain.Creators;
using HypeGrid.Domain.Campaigns;
using HypeGrid.Domain.Finance;

namespace HypeGrid.API.Controllers.Admin;

/// <summary>Creators — backs the admin Creators + CreatorDetail pages.</summary>
[Route("api/admin/creators")]
public sealed class CreatorsController : AdminCrudController<Creator>
{
    private readonly IRepository<Deliverable> _deliverables;
    private readonly IRepository<Payout> _payouts;

    public CreatorsController(
        IRepository<Creator> repo,
        IRepository<Deliverable> deliverables,
        IRepository<Payout> payouts) : base(repo)
    {
        _deliverables = deliverables;
        _payouts = payouts;
    }

    protected override Expression<Func<Creator, bool>> BuildSearchPredicate(string q)
        => c => c.FullName.Contains(q) || (c.Handle != null && c.Handle.Contains(q));

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] StatusPatchDto dto, CancellationToken ct)
    {
        var creator = await Repo.GetByIdAsync(id, ct);
        if (creator is null) return MapNotFound();
        creator.Status = dto.Status;
        Repo.Update(creator);
        await Repo.SaveChangesAsync(ct);
        return Data(creator, "Status updated.");
    }

    [HttpGet("{id:guid}/deliverables")]
    public async Task<IActionResult> Deliverables(Guid id, CancellationToken ct)
        => Data(await _deliverables.ListAsync("-created_date", null, d => d.CreatorId == id, ct));

    [HttpGet("{id:guid}/payouts")]
    public async Task<IActionResult> Payouts(Guid id, CancellationToken ct)
        => Data(await _payouts.ListAsync("-created_date", null, p => p.CreatorId == id, ct));
}
