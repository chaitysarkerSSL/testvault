using TestVault.Application.DTOs;
using TestVault.Application.Exceptions;
using TestVault.Application.Interfaces;
using TestVault.Domain.Entities;

namespace TestVault.Application.Services;

/// <summary>
/// Business logic for runs, ported from backend/routes/runs.js. Composes
/// IRunsRepository and ITestCasesRepository - no SQL, no HTTP, no
/// controller/framework dependency.
/// </summary>
public class RunsService
{
    private readonly IRunsRepository _runsRepository;
    private readonly ITestCasesRepository _testCasesRepository;

    public RunsService(IRunsRepository runsRepository, ITestCasesRepository testCasesRepository)
    {
        _runsRepository = runsRepository;
        _testCasesRepository = testCasesRepository;
    }

    /// <summary>Today's stats alone. Thin pass-through, exposed separately from GetDashboardSummaryAsync for standalone reuse.</summary>
    public Task<RunSummaryDto> GetTodayStatisticsAsync(CancellationToken cancellationToken = default)
        => _runsRepository.GetTodaySummaryAsync(cancellationToken);

    /// <summary>The 14-day trend alone. Thin pass-through, exposed separately from GetDashboardSummaryAsync for standalone reuse.</summary>
    public Task<IReadOnlyList<RunTrendPointDto>> GetTrendDataAsync(CancellationToken cancellationToken = default)
        => _runsRepository.GetTrendAsync(cancellationToken);

    /// <summary>
    /// The dashboard's combined response: today's stats + trend, run as two
    /// independent repository calls and composed here - exactly what
    /// GET /api/runs/stats/summary does in backend/routes/runs.js:7-65.
    /// </summary>
    public async Task<DashboardDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        var today = await GetTodayStatisticsAsync(cancellationToken);
        var trend = await GetTrendDataAsync(cancellationToken);

        return new DashboardDto { Today = today, Trend = trend };
    }

    /// <summary>
    /// Most recent 50 runs. Ported from GET /api/runs
    /// (backend/routes/runs.js:70-81). No business rules to apply beyond
    /// mapping to RunListItemDto - see that type for why the raw TestRun
    /// entity isn't returned directly (Phase 4 compatibility fix).
    /// </summary>
    public async Task<IReadOnlyList<RunListItemDto>> GetRunListAsync(CancellationToken cancellationToken = default)
    {
        var runs = await _runsRepository.GetRunListAsync(cancellationToken);
        return runs.Select(RunListItemDto.FromEntity).ToList();
    }

    /// <summary>
    /// A run with its test cases. Ported from GET /api/runs/:id
    /// (backend/routes/runs.js:86-119): fetches the run and its test cases
    /// as two separate repository calls and combines them, exactly like the
    /// Node route does with its two awaited queries.
    /// </summary>
    /// <exception cref="NotFoundException">No run exists with this id (ported from runs.js:107-109's 404).</exception>
    public async Task<RunDetailDto> GetRunDetailAsync(string runId, CancellationToken cancellationToken = default)
    {
        var run = await _runsRepository.GetRunDetailAsync(runId, cancellationToken);
        if (run is null)
        {
            throw new NotFoundException($"Run '{runId}' was not found.");
        }

        var tests = await _testCasesRepository.GetByRunIdAsync(runId, cancellationToken);

        return new RunDetailDto
        {
            Id = run.Id,
            Total = run.Total,
            Passed = run.Passed,
            Failed = run.Failed,
            Skipped = run.Skipped,
            TriggeredBy = run.TriggeredBy,
            Environment = run.Environment,
            Browser = run.Browser,
            Status = run.Status,
            StartedAt = run.StartedAt,
            FinishedAt = run.FinishedAt,
            DurationMs = run.DurationMs,
            Tests = tests.Select(TestDetailDto.FromEntity).ToList()
        };
    }
}
