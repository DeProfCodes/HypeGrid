using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using HypeGrid.Application.Common.Interfaces;
using HypeGrid.Domain.Creators;
using HypeGrid.Domain.Leads;

namespace HypeGrid.API.Controllers.Admin;

/// <summary>
/// Creator applications — backs the admin review queue, plus approve / reject
/// and convert-to-creator.
/// </summary>
[Route("api/admin/creator-applications")]
public sealed class CreatorApplicationsController : AdminCrudController<CreatorApplication>
{
    private readonly IRepository<Creator> _creators;

    public CreatorApplicationsController(
        IRepository<CreatorApplication> repo,
        IRepository<Creator> creators) : base(repo)
    {
        _creators = creators;
    }

    protected override Expression<Func<CreatorApplication, bool>> BuildSearchPredicate(string q)
        => a => a.FullName.Contains(q) || a.Email.Contains(q);

    [HttpPatch("{id:guid}/status")]
    public Task<IActionResult> SetStatus(Guid id, [FromBody] StatusPatchDto dto, CancellationToken ct)
        => TransitionAsync(id, dto.Status, ct);

    [HttpPost("{id:guid}/approve")]
    public Task<IActionResult> Approve(Guid id, CancellationToken ct) => TransitionAsync(id, "Approved", ct);

    [HttpPost("{id:guid}/reject")]
    public Task<IActionResult> Reject(Guid id, CancellationToken ct) => TransitionAsync(id, "Rejected", ct);

    /// <summary>Creates a Creator (status Approved) from an application.</summary>
    [HttpPost("{id:guid}/convert-to-creator")]
    public async Task<IActionResult> ConvertToCreator(Guid id, CancellationToken ct)
    {
        var app = await Repo.GetByIdAsync(id, ct);
        if (app is null) return MapNotFound();

        var creator = new Creator
        {
            FullName = app.FullName,
            Email = app.Email,
            Phone = app.Phone,
            Handle = app.HandleOrProfileLink,
            Platform = app.MainPlatform,
            Niche = app.ContentNiche,
            City = app.City,
            Province = app.Province,
            Bio = app.ApplicationReason,
            Status = "Approved"
        };
        await _creators.AddAsync(creator, ct);

        app.Status = "Approved";
        Repo.Update(app);
        await _creators.SaveChangesAsync(ct);

        return Data(creator, "Converted to creator.");
    }

    private async Task<IActionResult> TransitionAsync(Guid id, string status, CancellationToken ct)
    {
        var app = await Repo.GetByIdAsync(id, ct);
        if (app is null) return MapNotFound();
        app.Status = status;
        Repo.Update(app);
        await Repo.SaveChangesAsync(ct);
        return Data(app, "Status updated.");
    }
}
