using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using HypeGrid.Application.Common.Interfaces;
using HypeGrid.Domain.Campaigns;
using HypeGrid.Domain.Clients;
using HypeGrid.Domain.Finance;

namespace HypeGrid.API.Controllers.Admin;

/// <summary>
/// Clients — backs HypeGridAdmin Clients list + ClientDetail page (Campaigns /
/// Quotes / Invoices tabs served as sub-resource GETs filtered by client id).
/// </summary>
[Route("api/admin/clients")]
public sealed class ClientsController : AdminCrudController<Client>
{
    private readonly IRepository<Campaign> _campaigns;
    private readonly IRepository<Quote> _quotes;
    private readonly IRepository<Invoice> _invoices;

    public ClientsController(
        IRepository<Client> repo,
        IRepository<Campaign> campaigns,
        IRepository<Quote> quotes,
        IRepository<Invoice> invoices) : base(repo)
    {
        _campaigns = campaigns;
        _quotes = quotes;
        _invoices = invoices;
    }

    protected override Expression<Func<Client, bool>> BuildSearchPredicate(string q)
        => c => c.BrandName.Contains(q) || c.ContactPerson.Contains(q);

    [HttpGet("{id:guid}/campaigns")]
    public async Task<IActionResult> Campaigns(Guid id, CancellationToken ct)
        => Data(await _campaigns.ListAsync("-created_date", null, c => c.ClientId == id, ct));

    [HttpGet("{id:guid}/quotes")]
    public async Task<IActionResult> Quotes(Guid id, CancellationToken ct)
        => Data(await _quotes.ListAsync("-created_date", null, qte => qte.ClientId == id, ct));

    [HttpGet("{id:guid}/invoices")]
    public async Task<IActionResult> Invoices(Guid id, CancellationToken ct)
        => Data(await _invoices.ListAsync("-created_date", null, i => i.ClientId == id, ct));
}
