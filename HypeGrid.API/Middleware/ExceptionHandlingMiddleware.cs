using System.Text.Json;
using HypeGrid.Shared.Errors;

namespace HypeGrid.API.Middleware;

/// <summary>
/// Converts unhandled exceptions into the standard <c>{ success, code, message }</c>
/// envelope so clients never receive a raw stack trace. Detailed errors are
/// logged server-side; the client sees a generic message.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var payload = JsonSerializer.Serialize(new
            {
                success = false,
                code = ErrorCodes.Exception,
                message = "An unexpected error occurred."
            });

            await context.Response.WriteAsync(payload);
        }
    }
}
