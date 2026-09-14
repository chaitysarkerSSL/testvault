using Microsoft.AspNetCore.Mvc;
using TestVault.Application.DTOs;
using TestVault.Application.Services;

namespace TestVault.Web.Controllers;

/// <summary>
/// Run-level endpoints. Replaces backend/routes/runs.js, mounted at
/// /api/runs in the original Express app (backend/server.js:30).
/// </summary>
[ApiController]
[Route("api/runs")]
public class RunsController : ControllerBase
{
    private readonly RunsService _runsService;

    public RunsController(RunsService runsService)
    {
        _runsService = runsService;
    }

    /// <summary>
    /// Today's stats plus the 14-day trend, combined. Exact port of
    /// GET /api/runs/stats/summary (backend/routes/runs.js:7-65).
    /// </summary>
    /// <response code="200">Returns the dashboard summary.</response>
    [HttpGet("stats/summary")]
    [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardDto>> GetDashboardSummary(CancellationToken cancellationToken)
    {
        var summary = await _runsService.GetDashboardSummaryAsync(cancellationToken);
        return Ok(summary);
    }

    /// <summary>
    /// The 14-day pass-rate trend on its own. Not a separate endpoint in the
    /// original Node app (trend only ever appeared embedded in
    /// GET /stats/summary) - added here because this phase's spec calls for
    /// a standalone "Trend" endpoint.
    /// </summary>
    /// <response code="200">Returns the trend points, oldest first.</response>
    [HttpGet("stats/trend")]
    [ProducesResponseType(typeof(IReadOnlyList<RunTrendPointDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RunTrendPointDto>>> GetTrend(CancellationToken cancellationToken)
    {
        var trend = await _runsService.GetTrendDataAsync(cancellationToken);
        return Ok(trend);
    }

    /// <summary>
    /// Most recent 50 runs, newest first. Exact port of GET /api/runs
    /// (backend/routes/runs.js:70-81).
    /// </summary>
    /// <response code="200">Returns the run list.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RunListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RunListItemDto>>> GetRunList(CancellationToken cancellationToken)
    {
        var runs = await _runsService.GetRunListAsync(cancellationToken);
        return Ok(runs);
    }

    /// <summary>
    /// A single run with its test cases. Exact port of GET /api/runs/:id
    /// (backend/routes/runs.js:86-119). Returns 404 (via
    /// NotFoundException + ExceptionHandlingMiddleware) if the run doesn't
    /// exist, matching the original's res.status(404).
    /// </summary>
    /// <param name="id">The run id (a GUID string produced by the Node runner).</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <response code="200">Returns the run and its test cases.</response>
    /// <response code="404">No run exists with this id.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(RunDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RunDetailDto>> GetRunDetail(string id, CancellationToken cancellationToken)
    {
        var run = await _runsService.GetRunDetailAsync(id, cancellationToken);
        return Ok(run);
    }
}
