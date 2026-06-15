using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using HypeGrid.Application.Common.Interfaces;
using HypeGrid.Domain.Campaigns;
using HypeGrid.Domain.Finance;

namespace HypeGrid.API.Controllers.Admin;

/// <summary>
/// Campaigns — backs the admin Campaigns list and the CampaignDetail page,
/// including its Tasks / Deliverables / Payments / Notes tabs (served as
/// sub-resource GETs filtered by campaign id).
/// </summary>
[Route("api/admin/campaigns")]
public sealed class CampaignsController : AdminCrudController<Campaign>
{
    private readonly IRepository<CampaignTask> _tasks;
    private readonly IRepository<Deliverable> _deliverables;
    private readonly IRepository<Note> _notes;
    private readonly IRepository<Invoice> _invoices;

    public CampaignsController(
        IRepository<Campaign> repo,
        IRepository<CampaignTask> tasks,
        IRepository<Deliverable> deliverables,
        IRepository<Note> notes,
        IRepository<Invoice> invoices) : base(repo)
    {
        _tasks = tasks;
        _deliverables = deliverables;
        _notes = notes;
        _invoices = invoices;
    }

    protected override Expression<Func<Campaign, bool>> BuildSearchPredicate(string q)
        => c => c.Name.Contains(q) || (c.ClientName != null && c.ClientName.Contains(q));

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] StatusPatchDto dto, CancellationToken ct)
    {
        var campaign = await Repo.GetByIdAsync(id, ct);
        if (campaign is null) return MapNotFound();
        campaign.Status = dto.Status;
        Repo.Update(campaign);
        await Repo.SaveChangesAsync(ct);
        return Data(campaign, "Status updated.");
    }

    [HttpPatch("{id:guid}/progress")]
    public async Task<IActionResult> SetProgress(Guid id, [FromBody] ProgressPatchDto dto, CancellationToken ct)
    {
        var campaign = await Repo.GetByIdAsync(id, ct);
        if (campaign is null) return MapNotFound();
        campaign.Progress = Math.Clamp(dto.Progress, 0, 100);
        Repo.Update(campaign);
        await Repo.SaveChangesAsync(ct);
        return Data(campaign, "Progress updated.");
    }

    [HttpGet("{id:guid}/tasks")]
    public async Task<IActionResult> Tasks(Guid id, CancellationToken ct)
        => Data(await _tasks.ListAsync("-created_date", null, t => t.CampaignId == id, ct));

    [HttpGet("{id:guid}/deliverables")]
    public async Task<IActionResult> Deliverables(Guid id, CancellationToken ct)
        => Data(await _deliverables.ListAsync("-created_date", null, d => d.CampaignId == id, ct));

    [HttpGet("{id:guid}/notes")]
    public async Task<IActionResult> Notes(Guid id, CancellationToken ct)
        => Data(await _notes.ListAsync("-created_date", null, n => n.EntityId == id, ct));

    [HttpGet("{id:guid}/invoices")]
    public async Task<IActionResult> Invoices(Guid id, CancellationToken ct)
        => Data(await _invoices.ListAsync("-created_date", null, i => i.CampaignId == id, ct));
}
