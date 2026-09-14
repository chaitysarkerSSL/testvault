namespace TestVault.Web.Middleware;

/// <summary>
/// Adds a baseline set of security response headers to every response.
/// New in Phase 6 - the original Node app (plain Express with only
/// app.use(cors(...)) and app.use(express.json())) set none of these.
///
/// Deliberately conservative: this is a JSON API with a separate React
/// frontend, not a server-rendered page, so most of what a header like
/// Content-Security-Policy exists to protect (inline scripts XSS on pages
/// this app itself renders) doesn't apply to API responses the same way -
/// a strict CSP is left to the frontend's own hosting config instead of
/// being duplicated/risked here (an overly strict CSP on this origin could
/// also break the Swagger UI page it serves in Development).
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Stops browsers from MIME-sniffing a response into executing as a
        // different content type than declared (e.g. a JSON error body
        // being sniffed and rendered as HTML).
        headers["X-Content-Type-Options"] = "nosniff";

        // This API is never meant to be framed - blocks clickjacking-style
        // embedding entirely.
        headers["X-Frame-Options"] = "DENY";

        // Send the full referrer only to this app's own origin; strip it
        // down to just the origin for cross-origin navigations/requests.
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // This API doesn't use any browser feature that needs opting into
        // (camera, geolocation, etc.) - disable the lot by default.
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        // The legacy browser XSS filter this header controlled is
        // deprecated and has its own history of introducing vulnerabilities
        // - current OWASP guidance is to explicitly disable it (not omit
        // the header, which some older browsers read as "use your default").
        headers["X-XSS-Protection"] = "0";

        return _next(context);
    }
}
