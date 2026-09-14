using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TestVault.Application.DTOs;
using TestVault.Application.Security;
using TestVault.Application.Services;

namespace TestVault.Web.Controllers;

/// <summary>
/// AI failure-analysis endpoints. Replaces backend/routes/analysis.js,
/// mounted at /api/analysis in the original Express app
/// (backend/server.js:33).
///
/// Phase 6: triggering analysis is restricted to Admin/Tester (Roles.AdminOrTester) -
/// it calls a paid external API (Claude) per request. Reading an existing
/// analysis is left at the class-level [Authorize] baseline (any
/// authenticated role) - it's a free read of already-computed data.
/// </summary>
[ApiController]
[Route("api/analysis")]
[Authorize]
public class AnalysisController : ControllerBase
{
    private readonly AiAnalysisService _aiAnalysisService;

    public AnalysisController(AiAnalysisService aiAnalysisService)
    {
        _aiAnalysisService = aiAnalysisService;
    }

    /// <summary>
    /// The existing AI analysis for a test case, if one has already been
    /// produced. Not a route in the original Node app (analysis.js only
    /// ever exposed the trigger endpoint below) - added here because this
    /// phase's spec calls for a standalone "Get existing analysis" endpoint.
    /// Returns 404 when the test case has not been analyzed yet.
    /// </summary>
    /// <param name="testCaseId">The test case's database id.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <response code="200">Returns the existing analysis.</response>
    /// <response code="404">This test case has not been analyzed yet.</response>
    [HttpGet("{testCaseId:int}")]
    [ProducesResponseType(typeof(AiAnalysisResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AiAnalysisResultDto>> GetExistingAnalysis(int testCaseId, CancellationToken cancellationToken)
    {
        var analysis = await _aiAnalysisService.GetExistingAnalysisAsync(testCaseId, cancellationToken);

        return analysis is null
            ? NotFound(new { success = false, message = $"Test case '{testCaseId}' has not been analyzed yet." })
            : Ok(analysis);
    }

    /// <summary>
    /// Validates the test case failed, asks the configured AI provider to
    /// analyze it, and persists the result. Exact port of
    /// POST /api/analysis/analyze/:testCaseId (backend/routes/analysis.js:6-79),
    /// including its route path. Returns 404 if the test case doesn't exist
    /// and 400 if it didn't fail (both via exceptions + ExceptionHandlingMiddleware,
    /// matching the original's status codes for those two branches).
    /// </summary>
    /// <param name="testCaseId">The failed test case's database id.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <response code="200">Returns the newly produced analysis.</response>
    /// <response code="400">The test case did not fail, so it cannot be analyzed.</response>
    /// <response code="404">No test case exists with this id.</response>
    [HttpPost("analyze/{testCaseId:int}")]
    [Authorize(Roles = Roles.AdminOrTester)]
    [ProducesResponseType(typeof(AiAnalysisResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AiAnalysisResultDto>> Analyze(int testCaseId, CancellationToken cancellationToken)
    {
        var analysis = await _aiAnalysisService.AnalyzeAsync(testCaseId, cancellationToken);

        // Status code (200) matches the original res.json(analysis), which
        // defaults to 200 - not 201, even though this creates/overwrites a
        // row, to preserve the existing contract exactly.
        return Ok(analysis);
    }
}
