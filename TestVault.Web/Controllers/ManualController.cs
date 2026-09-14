using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TestVault.Application.DTOs;
using TestVault.Application.Security;
using TestVault.Application.Services;
using TestVault.Web.Models;

namespace TestVault.Web.Controllers;

/// <summary>
/// Manual test-run endpoints. Replaces backend/routes/manual.js, mounted at
/// /api/manual in the original Express app (backend/server.js:32).
///
/// Every response's success-message text is preserved verbatim from the
/// original (same Bengali strings) - only the error-envelope shape changed,
/// per Phase 4's exception-handling design.
///
/// Phase 6: starting/stopping a run is restricted to Admin/Tester (Roles.AdminOrTester) -
/// running the test suite has real cost (spawns a process, calls out to a
/// real target site) and shouldn't be available to a read-only "User".
/// Checking status is left at the class-level [Authorize] baseline (any
/// authenticated role) - it's harmless read access.
/// </summary>
[ApiController]
[Route("api/manual")]
[Authorize]
public class ManualController : ControllerBase
{
    // Client-supplied environment overrides are allow-listed rather than
    // forwarded verbatim (as the original's `{ ...env }` spread did) -
    // blindly merging arbitrary client JSON into a spawned process's
    // environment is a real injection risk the original never guarded
    // against. Both of these are genuine, meaningful overrides the app
    // already reads elsewhere (NODE_ENV: scripts/run-tests.js:25,
    // TEST_BASE_URL: playwright.config.js's baseURL).
    private static readonly string[] AllowedEnvOverrideKeys = { "NODE_ENV", "TEST_BASE_URL" };

    private readonly ManualRunService _manualRunService;

    public ManualController(ManualRunService manualRunService)
    {
        _manualRunService = manualRunService;
    }

    /// <summary>
    /// Starts a manual test run in the background and returns immediately.
    /// Exact port of POST /api/manual/run (backend/routes/manual.js:9-32).
    /// Returns 409 (via ConflictException + ExceptionHandlingMiddleware) if
    /// a run is already active, matching the original's res.status(409).
    /// </summary>
    /// <param name="request">Optional environment variable overrides (allow-listed - see AllowedEnvOverrideKeys).</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <response code="200">The run was started; returns its id.</response>
    /// <response code="409">A manual run is already in progress.</response>
    [HttpPost("run")]
    [Authorize(Roles = Roles.AdminOrTester)]
    [ProducesResponseType(typeof(ManualRunStartResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ManualRunStartResponseDto>> StartRun([FromBody] ManualRunRequestDto? request, CancellationToken cancellationToken)
    {
        var environmentOverrides = SanitizeEnv(request?.Env);
        var runId = await _manualRunService.StartRunAsync(environmentOverrides, cancellationToken);

        // Message preserved verbatim from manual.js:19.
        return Ok(new ManualRunStartResponseDto { Message = "TestVault run শুরু হয়েছে!", RunId = runId });
    }

    /// <summary>
    /// Stops the currently active manual run. Exact port of
    /// POST /api/manual/stop (backend/routes/manual.js:70-78). Returns 400
    /// (via ValidationException + ExceptionHandlingMiddleware) if nothing
    /// is running, matching the original's res.status(400).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <response code="200">The run was stopped.</response>
    /// <response code="400">No run is currently active.</response>
    [HttpPost("stop")]
    [Authorize(Roles = Roles.AdminOrTester)]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MessageResponseDto>> StopRun(CancellationToken cancellationToken)
    {
        await _manualRunService.StopRunAsync(cancellationToken);

        // Message preserved verbatim from manual.js:77.
        return Ok(new MessageResponseDto { Message = "Run বন্ধ করা হয়েছে।" });
    }

    /// <summary>
    /// Whether a manual run is currently active. Exact port of
    /// GET /api/manual/status (backend/routes/manual.js:65-67).
    /// </summary>
    /// <response code="200">Returns the current execution status.</response>
    [HttpGet("status")]
    [ProducesResponseType(typeof(ManualRunStatusDto), StatusCodes.Status200OK)]
    public ActionResult<ManualRunStatusDto> GetStatus()
    {
        return Ok(_manualRunService.GetStatus());
    }

    private static IReadOnlyDictionary<string, string>? SanitizeEnv(Dictionary<string, string>? env)
    {
        if (env is null)
        {
            return null;
        }

        return env
            .Where(kv => AllowedEnvOverrideKeys.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }
}
