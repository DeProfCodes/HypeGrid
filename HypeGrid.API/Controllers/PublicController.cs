using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HypeGrid.Application.Common.Interfaces;
using HypeGrid.Application.Leads;
using HypeGrid.Application.Leads.Dtos;
using HypeGrid.Domain.Content;
using HypeGrid.Domain.Marketing;

namespace HypeGrid.API.Controllers;

/// <summary>
/// Public website endpoints — all anonymous. Serves CMS content that is
/// currently hardcoded in the website, and accepts the three conversion forms
/// (contact, campaign request, creator application) + newsletter, which
/// currently only set local React state with no backend.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/public")]
public sealed class PublicController : BaseController
{
    private readonly IPublicLeadService _leads;
    private readonly IRepository<Service> _services;
    private readonly IRepository<Package> _packages;
    private readonly IRepository<Testimonial> _testimonials;
    private readonly IRepository<CaseStudy> _caseStudies;
    private readonly IRepository<SiteSetting> _settings;
    private readonly IRepository<HeroPlacement> _heroes;
    private readonly IRepository<Deal> _deals;
    private readonly IRepository<FeaturedVideo> _featuredVideos;

    public PublicController(
        IPublicLeadService leads,
        IRepository<Service> services,
        IRepository<Package> packages,
        IRepository<Testimonial> testimonials,
        IRepository<CaseStudy> caseStudies,
        IRepository<SiteSetting> settings,
        IRepository<HeroPlacement> heroes,
        IRepository<Deal> deals,
        IRepository<FeaturedVideo> featuredVideos)
    {
        _leads = leads;
        _services = services;
        _packages = packages;
        _testimonials = testimonials;
        _caseStudies = caseStudies;
        _settings = settings;
        _heroes = heroes;
        _deals = deals;
        _featuredVideos = featuredVideos;
    }

    // ── Content ─────────────────────────────────────────────────────────────

    [HttpGet("services")]
    public async Task<IActionResult> GetServices(CancellationToken ct)
        => Data(await _services.ListAsync("sort_order", null, s => s.IsActive, ct));

    [HttpGet("services/{slug}")]
    public async Task<IActionResult> GetService(string slug, CancellationToken ct)
    {
        var service = await _services.FirstOrDefaultAsync(s => s.Slug == slug && s.IsActive, ct);
        return service is null ? MapNotFound("Service") : Data(service);
    }

    [HttpGet("packages")]
    public async Task<IActionResult> GetPackages(CancellationToken ct)
        => Data(await _packages.ListAsync("sort_order", null, p => p.IsActive, ct));

    [HttpGet("testimonials")]
    public async Task<IActionResult> GetTestimonials(CancellationToken ct)
        => Data(await _testimonials.ListAsync("sort_order", null, t => t.IsActive, ct));

    [HttpGet("case-studies")]
    public async Task<IActionResult> GetCaseStudies(CancellationToken ct)
        => Data(await _caseStudies.ListAsync("sort_order", null, c => c.IsActive, ct));

    [HttpGet("site-settings")]
    public async Task<IActionResult> GetSiteSettings(CancellationToken ct)
    {
        // Only expose public-facing groups (never finance/internal keys).
        var rows = await _settings.ListAsync("key", null, s => s.Group == "company" || s.Group == "communication", ct);
        var map = rows.ToDictionary(s => s.Key, s => s.Value);
        return Data(map);
    }

    // ── Marketing: hero placements / deals / featured video ──────────────────

    /// <summary>Active, in-date hero carousel slides, ordered by priority.</summary>
    [HttpGet("hero-placements")]
    public async Task<IActionResult> GetHeroPlacements(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        return Data(await _heroes.ListAsync("priority", 50,
            h => h.IsActive
                 && (h.StartDate == null || h.StartDate <= now)
                 && (h.EndDate == null || h.EndDate >= now), ct));
    }

    /// <summary>Active, non-expired deals (optionally filtered by category), ordered by priority.</summary>
    [HttpGet("deals")]
    public async Task<IActionResult> GetDeals([FromQuery] string? category, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var cat = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
        return Data(await _deals.ListAsync("priority", 200,
            d => d.IsActive
                 && (d.ValidFrom == null || d.ValidFrom <= now)
                 && (d.ValidUntil == null || d.ValidUntil >= now)
                 && (cat == null || d.Category == cat), ct));
    }

    [HttpGet("deals/{slug}")]
    public async Task<IActionResult> GetDeal(string slug, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var deal = await _deals.FirstOrDefaultAsync(
            d => d.Slug == slug && d.IsActive
                 && (d.ValidFrom == null || d.ValidFrom <= now)
                 && (d.ValidUntil == null || d.ValidUntil >= now), ct);
        return deal is null ? MapNotFound("Deal") : Data(deal);
    }

    /// <summary>The single active, in-date featured video (or null), lowest sort order.</summary>
    [HttpGet("featured-video")]
    public async Task<IActionResult> GetFeaturedVideo(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var rows = await _featuredVideos.ListAsync("sort_order", 1,
            v => v.IsActive
                 && (v.StartDate == null || v.StartDate <= now)
                 && (v.EndDate == null || v.EndDate >= now), ct);
        return Data(rows.FirstOrDefault());
    }

    // ── Conversion forms ────────────────────────────────────────────────────

    [HttpPost("contact")]
    public async Task<IActionResult> Contact([FromBody] ContactFormDto dto, CancellationToken ct)
        => ToActionResult(await _leads.SubmitContactAsync(dto, ct));

    [HttpPost("campaign-requests")]
    public async Task<IActionResult> CampaignRequest([FromBody] CampaignRequestFormDto dto, CancellationToken ct)
        => ToActionResult(await _leads.SubmitCampaignRequestAsync(dto, ct));

    [HttpPost("creator-applications")]
    public async Task<IActionResult> CreatorApplication([FromBody] CreatorApplicationFormDto dto, CancellationToken ct)
        => ToActionResult(await _leads.SubmitCreatorApplicationAsync(dto, ct));

    [HttpPost("newsletter/subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] NewsletterSubscribeDto dto, CancellationToken ct)
        => ToActionResult(await _leads.SubscribeNewsletterAsync(dto, ct));

    [HttpPost("newsletter/unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] NewsletterSubscribeDto dto, CancellationToken ct)
        => ToActionResult(await _leads.UnsubscribeNewsletterAsync(dto.Email, ct));

    private IActionResult MapNotFound(string what)
        => StatusCode(StatusCodes.Status404NotFound,
            new { success = false, code = "NOT_FOUND", message = $"{what} not found." });
}
