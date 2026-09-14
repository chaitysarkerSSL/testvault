using System.Text.Json;
using TestVault.Application.Exceptions;

namespace TestVault.Web.Middleware;

/// <summary>
/// Central exception handler for the whole request pipeline. Replaces the
/// repeated try/catch + res.status(...).json({ error: err.message }) blocks
/// in every Node route (backend/routes/runs.js, tests.js, analysis.js,
/// manual.js) with one place that maps Application-layer exceptions to
/// HTTP status codes.
///
/// The response envelope - { success: false, message } - is fixed by this
/// phase's spec and is a deliberate departure from the Node app's
/// { error: message } shape. It's also deliberately generic for
/// RepositoryException/unhandled exceptions: the real exception (which may
/// contain SQL/connection details) is always logged server-side via
/// <see cref="ILogger"/>, but never echoed to the client - unlike the
/// original Node routes, which returned err.message directly.
/// </summary>
public class ExceptionHandlingMiddleware
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
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            // 401 - AuthController's own login/refresh failures (invalid
            // credentials, locked out, bad/expired/reused refresh token).
            // Distinct from the framework's own 401 for a missing/invalid
            // bearer token on an [Authorize] endpoint, which never reaches
            // this switch - JwtBearer's own handler short-circuits before a
            // controller action (or this middleware's try block) runs.
            AuthenticationException => (StatusCodes.Status401Unauthorized, exception.Message),

            // 404 - ported from the "not found" branches in runs.js/analysis.js.
            NotFoundException => (StatusCodes.Status404NotFound, exception.Message),

            // 400 - ported from the "only failed tests can be analyzed" branch in analysis.js,
            // and the "no run is active" branch in manual.js.
            ValidationException => (StatusCodes.Status400BadRequest, exception.Message),

            // 409 - ported from the "a run is already active" branch in manual.js.
            ConflictException => (StatusCodes.Status409Conflict, exception.Message),

            // 500 - a known data-access failure. Message is generic on
            // purpose; RepositoryException.Message/InnerException may
            // contain SQL details that shouldn't reach the client.
            RepositoryException => (StatusCodes.Status500InternalServerError, "A data access error occurred. Please try again later."),

            // 500 - anything else unhandled.
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.")
        };

        _logger.LogError(
            exception,
            "Unhandled exception while processing {Method} {Path} -> {StatusCode}",
            context.Request.Method,
            context.Request.Path,
            statusCode);

        if (context.Response.HasStarted)
        {
            // Response body already partially written (e.g. streamed) -
            // nothing more we can do but log, which happened above.
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var payload = JsonSerializer.Serialize(new { success = false, message });
        await context.Response.WriteAsync(payload);
    }
}
