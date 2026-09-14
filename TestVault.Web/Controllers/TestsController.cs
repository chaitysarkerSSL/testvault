using Microsoft.AspNetCore.Mvc;
using TestVault.Application.DTOs;
using TestVault.Application.Services;

namespace TestVault.Web.Controllers;

/// <summary>
/// Individual test-case endpoints. Replaces backend/routes/tests.js,
/// mounted at /api/tests in the original Express app
/// (backend/server.js:31).
/// </summary>
[ApiController]
[Route("api/tests")]
public class TestsController : ControllerBase
{
    private readonly TestsService _testsService;

    public TestsController(TestsService testsService)
    {
        _testsService = testsService;
    }

    /// <summary>
    /// All test cases for one run. Not a standalone route in the original
    /// Node app (the same query only ever appeared embedded in
    /// GET /api/runs/:id) - added here because this phase's spec calls for
    /// a standalone "Tests by run" endpoint.
    /// </summary>
    /// <param name="runId">The owning run's id.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <response code="200">Returns the test cases for this run.</response>
    [HttpGet("by-run/{runId}")]
    [ProducesResponseType(typeof(IReadOnlyList<TestDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TestDetailDto>>> GetTestsByRun(string runId, CancellationToken cancellationToken)
    {
        var tests = await _testsService.GetTestsByRunIdAsync(runId, cancellationToken);
        return Ok(tests);
    }

    /// <summary>
    /// Most recent 20 failed tests. Exact port of GET /api/tests/failed
    /// (backend/routes/tests.js:6-25).
    /// </summary>
    /// <response code="200">Returns the failed tests.</response>
    [HttpGet("failed")]
    [ProducesResponseType(typeof(IReadOnlyList<FailedTestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FailedTestDto>>> GetFailedTests(CancellationToken cancellationToken)
    {
        var tests = await _testsService.GetFailedTestsAsync(cancellationToken);
        return Ok(tests);
    }

    /// <summary>
    /// Top 10 slowest test titles by average duration. Exact port of
    /// GET /api/tests/slowest (backend/routes/tests.js:28-41).
    /// </summary>
    /// <response code="200">Returns the slowest tests.</response>
    [HttpGet("slowest")]
    [ProducesResponseType(typeof(IReadOnlyList<SlowestTestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SlowestTestDto>>> GetSlowestTests(CancellationToken cancellationToken)
    {
        var tests = await _testsService.GetSlowestTestsAsync(cancellationToken);
        return Ok(tests);
    }

    /// <summary>
    /// A single test case by id. Not a standalone route in the original
    /// Node app (the same bare lookup only ever happened inside
    /// analysis.js's own handler) - added here because this phase's spec
    /// calls for a standalone "Test detail" endpoint. Returns 404 (via
    /// NotFoundException + ExceptionHandlingMiddleware) if it doesn't exist.
    /// </summary>
    /// <param name="id">The test case's database id.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <response code="200">Returns the test case.</response>
    /// <response code="404">No test case exists with this id.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TestDetailDto>> GetTestDetail(int id, CancellationToken cancellationToken)
    {
        var test = await _testsService.GetTestDetailAsync(id, cancellationToken);
        return Ok(test);
    }
}
