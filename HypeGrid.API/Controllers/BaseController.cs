using Microsoft.AspNetCore.Mvc;
using HypeGrid.Shared.Results;

namespace HypeGrid.API.Controllers;

/// <summary>
/// Base API controller. Converts the service-layer <see cref="Result"/> envelope
/// into HTTP responses with a consistent <c>{ success, message, data }</c> body,
/// mapping error codes to status codes centrally.
/// </summary>
[ApiController]
public abstract class BaseController : ControllerBase
{
    protected IActionResult ToActionResult(Result result)
    {
        if (result.IsSuccess)
            return Ok(new { success = true, message = result.Message });

        return MapFailure(result.Code, result.Message);
    }

    protected IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(new { success = true, message = result.Message, data = result.Data });

        return MapFailure(result.Code, result.Message);
    }

    /// <summary>Wraps a plain payload in the success envelope (for read endpoints).</summary>
    protected IActionResult Data<T>(T data, string message = "Success")
        => Ok(new { success = true, message, data });

    private IActionResult MapFailure(string code, string message)
    {
        var status = code switch
        {
            "BAD_REQUEST" => StatusCodes.Status400BadRequest,
            "VALIDATION_ERROR" => StatusCodes.Status400BadRequest,
            "WEAK_PASSWORD" => StatusCodes.Status400BadRequest,
            "INVALID_RESET_TOKEN" => StatusCodes.Status400BadRequest,

            "UNAUTHORIZED" => StatusCodes.Status401Unauthorized,
            "INVALID_CREDENTIALS" => StatusCodes.Status401Unauthorized,
            "INVALID_REFRESH_TOKEN" => StatusCodes.Status401Unauthorized,
            "REFRESH_TOKEN_EXPIRED" => StatusCodes.Status401Unauthorized,

            "FORBIDDEN" => StatusCodes.Status403Forbidden,
            "INACTIVE_ACCOUNT" => StatusCodes.Status403Forbidden,
            "EMAIL_NOT_CONFIRMED" => StatusCodes.Status403Forbidden,

            "NOT_FOUND" => StatusCodes.Status404NotFound,

            "CONFLICT" => StatusCodes.Status409Conflict,
            "EMAIL_TAKEN" => StatusCodes.Status409Conflict,

            "TOO_MANY_REQUESTS" => StatusCodes.Status429TooManyRequests,

            // Upstream-provider failures map to 422 (not 5xx) so a reverse proxy
            // doesn't swallow the structured envelope.
            "EMAIL_SEND_FAILED" => StatusCodes.Status422UnprocessableEntity,
            "PROVIDER_NOT_CONFIGURED" => StatusCodes.Status422UnprocessableEntity,

            _ => StatusCodes.Status500InternalServerError
        };

        return StatusCode(status, new { success = false, code, message });
    }
}
