using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using HypeGrid.Application.Analytics;
using HypeGrid.Application.Common.Interfaces;
using HypeGrid.Domain.Marketing;

namespace HypeGrid.API.Controllers.Admin;

/// <summary>Homepage hero/carousel placements — backs the admin "Hero Ads" page.</summary>
[Route("api/admin/hero-placements")]
public sealed class HeroPlacementsController : AdminCrudController<HeroPlacement>
{
    private readonly IAnalyticsService _analytics;

    public HeroPlacementsController(IRepository<HeroPlacement> repo, IAnalyticsService analytics) : base(repo)
        => _analytics = analytics;

    protected override Expression<Func<HeroPlacement, bool>> BuildSearchPredicate(string q)
        => h => h.Title.Contains(q) || (h.SponsorName != null && h.SponsorName.Contains(q));

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetActive(Guid id, [FromBody] ActivePatchDto dto, CancellationToken ct)
    {
        var hero = await Repo.GetByIdAsync(id, ct);
        if (hero is null) return MapNotFound();
        hero.IsActive = dto.IsActive;
        Repo.Update(hero);
        await Repo.SaveChangesAsync(ct);
        return Data(hero, "Status updated.");
    }

    [HttpGet("{id:guid}/analytics")]
    public async Task<IActionResult> Analytics(Guid id, CancellationToken ct)
        => Data(await _analytics.GetEntitySummaryAsync("HeroPlacement", id, ct));
}

/// <summary>Deals / specials / discounts — backs the admin "Deals" page.</summary>
[Route("api/admin/deals")]
public sealed class DealsController : AdminCrudController<Deal>
{
    private readonly IAnalyticsService _analytics;

    public DealsController(IRepository<Deal> repo, IAnalyticsService analytics) : base(repo)
        => _analytics = analytics;

    protected override Expression<Func<Deal, bool>> BuildSearchPredicate(string q)
        => d => d.Title.Contains(q) || (d.BrandName != null && d.BrandName.Contains(q));

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetActive(Guid id, [FromBody] ActivePatchDto dto, CancellationToken ct)
    {
        var deal = await Repo.GetByIdAsync(id, ct);
        if (deal is null) return MapNotFound();
        deal.IsActive = dto.IsActive;
        Repo.Update(deal);
        await Repo.SaveChangesAsync(ct);
        return Data(deal, "Status updated.");
    }

    [HttpPatch("{id:guid}/featured")]
    public async Task<IActionResult> SetFeatured(Guid id, [FromBody] FeaturedPatchDto dto, CancellationToken ct)
    {
        var deal = await Repo.GetByIdAsync(id, ct);
        if (deal is null) return MapNotFound();
        deal.IsFeatured = dto.IsFeatured;
        Repo.Update(deal);
        await Repo.SaveChangesAsync(ct);
        return Data(deal, "Featured updated.");
    }

    [HttpGet("{id:guid}/analytics")]
    public async Task<IActionResult> Analytics(Guid id, CancellationToken ct)
        => Data(await _analytics.GetEntitySummaryAsync("Deal", id, ct));
}

/// <summary>
/// Featured homepage videos — backs the admin "Featured Video" page. Modelled as
/// a full CRUD resource (not a singleton PUT) so it composes with the admin's
/// entity API shim; the public site shows the single active, in-date video.
/// </summary>
[Route("api/admin/featured-videos")]
public sealed class FeaturedVideosController : AdminCrudController<FeaturedVideo>
{
    public FeaturedVideosController(IRepository<FeaturedVideo> repo) : base(repo) { }

    protected override Expression<Func<FeaturedVideo, bool>> BuildSearchPredicate(string q)
        => v => v.Title.Contains(q);

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetActive(Guid id, [FromBody] ActivePatchDto dto, CancellationToken ct)
    {
        var video = await Repo.GetByIdAsync(id, ct);
        if (video is null) return MapNotFound();
        video.IsActive = dto.IsActive;
        Repo.Update(video);
        await Repo.SaveChangesAsync(ct);
        return Data(video, "Status updated.");
    }
}
