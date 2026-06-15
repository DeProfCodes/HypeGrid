using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HypeGrid.Application.Analytics;
using HypeGrid.Application.Analytics.Dtos;

namespace HypeGrid.API.Controllers;

/// <summary>
/// Anonymous, lightweight analytics ingestion for the public website. The IP and
/// user agent are read server-side (the IP is hashed, never stored raw). This
/// endpoint is intentionally forgiving — it must never break the page that calls
/// it, so failures are swallowed and still return a 200/202.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/public/analytics")]
public sealed class PublicAnalyticsController : BaseController
{
    private readonly IAnalyticsService _analytics;
    private readonly ILogger<PublicAnalyticsController> _logger;

    public PublicAnalyticsController(IAnalyticsService analytics, ILogger<PublicAnalyticsController> logger)
    {
        _analytics = analytics;
        _logger = logger;
    }

    [HttpPost("events")]
    public async Task<IActionResult> Record([FromBody] AnalyticsEventInput input, CancellationToken ct)
    {
        try
        {
            var ua = Request.Headers.UserAgent.ToString();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _analytics.RecordAsync(input, ua, ip, ct);
            // Even a validation failure returns 202 — the client is fire-and-forget.
            return Accepted(new { success = result.IsSuccess });
        }
        catch (Exception ex)
        {
            // Never surface analytics failures to the public page.
            _logger.LogWarning(ex, "Analytics event could not be recorded.");
            return Accepted(new { success = false });
        }
    }
}
