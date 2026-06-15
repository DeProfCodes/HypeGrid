using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using HypeGrid.Application.Common.Interfaces;
using HypeGrid.Domain.Campaigns;
using HypeGrid.Domain.Clients;
using HypeGrid.Domain.Leads;

namespace HypeGrid.API.Controllers.Admin;

/// <summary>
/// Campaign requests — backs the admin Requests page, plus status/assign and
/// the convert-to-client / convert-to-campaign actions.
/// </summary>
[Route("api/admin/campaign-requests")]
public sealed class RequestsController : AdminCrudController<CampaignRequest>
{
    private readonly IRepository<Client> _clients;
    private readonly IRepository<Campaign> _campaigns;

    public RequestsController(
        IRepository<CampaignRequest> repo,
        IRepository<Client> clients,
        IRepository<Campaign> campaigns) : base(repo)
    {
        _clients = clients;
        _campaigns = campaigns;
    }

    protected override Expression<Func<CampaignRequest, bool>> BuildSearchPredicate(string q)
        => r => r.FullName.Contains(q) || (r.BrandName != null && r.BrandName.Contains(q));

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] StatusPatchDto dto, CancellationToken ct)
    {
        var request = await Repo.GetByIdAsync(id, ct);
        if (request is null) return MapNotFound();
        request.Status = dto.Status;
        Repo.Update(request);
        await Repo.SaveChangesAsync(ct);
        return Data(request, "Status updated.");
    }

    [HttpPatch("{id:guid}/assign")]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignPatchDto dto, CancellationToken ct)
    {
        var request = await Repo.GetByIdAsync(id, ct);
        if (request is null) return MapNotFound();
        request.AssignedToUserId = dto.AssignedToUserId;
        Repo.Update(request);
        await Repo.SaveChangesAsync(ct);
        return Data(request, "Assigned.");
    }

    /// <summary>Creates a Client (status Lead) from a campaign request.</summary>
    [HttpPost("{id:guid}/convert-to-client")]
    public async Task<IActionResult> ConvertToClient(Guid id, CancellationToken ct)
    {
        var request = await Repo.GetByIdAsync(id, ct);
        if (request is null) return MapNotFound();

        var client = new Client
        {
            BrandName = string.IsNullOrWhiteSpace(request.BrandName) ? request.FullName : request.BrandName!,
            ContactPerson = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            Status = "Lead"
        };
        await _clients.AddAsync(client, ct);

        request.Status = "Converted";
        Repo.Update(request);
        await _clients.SaveChangesAsync(ct);

        return Data(client, "Converted to client.");
    }

    /// <summary>Creates a draft Campaign from a campaign request.</summary>
    [HttpPost("{id:guid}/convert-to-campaign")]
    public async Task<IActionResult> ConvertToCampaign(Guid id, CancellationToken ct)
    {
        var request = await Repo.GetByIdAsync(id, ct);
        if (request is null) return MapNotFound();

        var campaign = new Campaign
        {
            Name = $"{request.BrandName ?? request.FullName} campaign",
            ClientName = request.BrandName ?? request.FullName,
            Type = request.CampaignType,
            Status = "Requested",
            Phase = "Request Received",
            TargetAudience = request.TargetAudience,
            Platforms = request.Platforms,
            Brief = request.Message,
            Objective = request.CampaignGoal
        };
        await _campaigns.AddAsync(campaign, ct);

        request.Status = "Converted";
        Repo.Update(request);
        await _campaigns.SaveChangesAsync(ct);

        return Data(campaign, "Converted to campaign.");
    }
}
