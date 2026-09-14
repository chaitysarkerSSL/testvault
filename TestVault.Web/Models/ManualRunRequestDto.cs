namespace TestVault.Web.Models;

/// <summary>
/// Request body for POST /api/manual/run. Matches the original's
/// req.body.env (backend/routes/manual.js:17) - a bare, HTTP-binding-only
/// shape, which is why it lives in TestVault.Web rather than as an
/// Application DTO (ManualRunService's own method takes a plain
/// IReadOnlyDictionary, with no ASP.NET Core model-binding concerns).
/// </summary>
public class ManualRunRequestDto
{
    public Dictionary<string, string>? Env { get; set; }
}
