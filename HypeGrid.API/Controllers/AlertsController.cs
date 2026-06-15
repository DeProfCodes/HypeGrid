using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HypeGrid.Application.Alerts;
using HypeGrid.Application.Alerts.Dtos;
using HypeGrid.Application.Analytics;
using HypeGrid.Application.Analytics.Dtos;
using HypeGrid.Shared.Constants;

namespace HypeGrid.API.Controllers;

/// <summary>
/// Public HypeGrid Alerts / Deals Club endpoints — all anonymous. A visitor
/// submits the opt-in questionnaire (consent required), is shown a CTA to join
/// the public WhatsApp Channel, and we record only that they tapped it. No
/// WhatsApp messages are sent in phase 1.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/public/alerts")]
public sealed class AlertsController : BaseController
{
    private readonly IAlertSubscriptionService _alerts;
    private readonly IAnalyticsService _analytics;
    private readonly ILogger<AlertsController> _logger;

    public AlertsController(
        IAlertSubscriptionService alerts,
        IAnalyticsService analytics,
        ILogger<AlertsController> logger)
    {
        _alerts = alerts;
        _analytics = analytics;
        _logger = logger;
    }

    /// <summary>Static option lists for the public form (interests / frequencies / provinces).</summary>
    [HttpGet("options")]
    public IActionResult Options() => Data(new AlertOptionsDto
    {
        Interests = HypeGridValues.DealCategories,
        Frequencies = HypeGridValues.AlertFrequencies,
        Provinces = HypeGridValues.SaProvinces,
    });

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] AlertSubscribeDto dto, CancellationToken ct)
    {
        var ua = Request.Headers.UserAgent.ToString();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _alerts.SubscribeAsync(dto, ua, ip, ct);

        // Best-effort analytics — never block or fail the opt-in on a tracking hiccup.
        if (result.IsSuccess && result.Data is not null)
            await TrackAsync("alert_subscribe", result.Data.Id, dto.SourcePage, ua, ip, ct);

        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/channel-click")]
    public async Task<IActionResult> ChannelClick(Guid id, CancellationToken ct)
    {
        var ua = Request.Headers.UserAgent.ToString();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _alerts.MarkChannelClickedAsync(id, ct);

        if (result.IsSuccess)
            await TrackAsync("whatsapp_click", id, null, ua, ip, ct);

        return ToActionResult(result);
    }

    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] AlertUnsubscribeDto dto, CancellationToken ct)
        => ToActionResult(await _alerts.UnsubscribeAsync(dto.Email, dto.Phone, ct));

    private async Task TrackAsync(string eventType, Guid id, string? sourcePage, string? ua, string? ip, CancellationToken ct)
    {
        try
        {
            await _analytics.RecordAsync(new AnalyticsEventInput
            {
                EventType = eventType,
                EntityType = "AlertSubscription",
                EntityId = id,
                PagePath = sourcePage,
            }, ua, ip, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Alerts analytics ({EventType}) could not be recorded.", eventType);
        }
    }
}
