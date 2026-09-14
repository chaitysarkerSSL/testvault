using TestVault.Application.DTOs;
using TestVault.Domain.Entities;
using TestVault.Domain.Enums;

namespace TestVault.Application.Interfaces;

/// <summary>
/// Read/write access to test_runs. Ported from backend/routes/runs.js.
///
/// Mostly read-only: test_runs rows are still populated primarily by the
/// existing, unchanged Node.js/Playwright reporter (reporters/sql-reporter.js)
/// - neither it nor the test-execution engine is being replaced in this
/// migration (see the "test-execution bridge" decision from Phase 0). The
/// one exception is <see cref="CreatePlaceholderRunAsync"/>, added in
/// Phase 5: ManualRunService now owns the "register a running placeholder
/// row before the test process even starts" responsibility that
/// scripts/run-tests.js's startRun() used to have for manual runs.
/// </summary>
public interface IRunsRepository
{
    /// <summary>
    /// Today's run stats (count, passed, failed, average pass rate).
    /// Ported from the "todayResult" query, GET /api/runs/stats/summary
    /// (backend/routes/runs.js:12-33).
    /// </summary>
    Task<RunSummaryDto> GetTodaySummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Last 14 days of pass-rate trend data, oldest first.
    /// Ported from the "trendResult" query, GET /api/runs/stats/summary
    /// (backend/routes/runs.js:36-59).
    /// </summary>
    Task<IReadOnlyList<RunTrendPointDto>> GetTrendAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Most recent 50 runs, newest first. Ported from GET /api/runs
    /// (backend/routes/runs.js:70-81). Does not populate TestCases.
    /// </summary>
    Task<IReadOnlyList<TestRun>> GetRunListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// A single run by id, or null if it doesn't exist. Ported from the run
    /// lookup half of GET /api/runs/:id (backend/routes/runs.js:90-92). Does
    /// not populate TestCases - see ITestCasesRepository.GetByRunIdAsync for
    /// the other half of that endpoint.
    /// </summary>
    Task<TestRun?> GetRunDetailAsync(string runId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new "running" placeholder row for a run that's about to
    /// start, if one doesn't already exist for this id. Near-verbatim port
    /// of the guarded INSERT in scripts/run-tests.js's startRun()
    /// (lines 22-33) - reporters/sql-reporter.js's onBegin performs the
    /// same existence check independently when the actual Playwright
    /// process starts moments later (it sets `total`), so both guards
    /// matter: this one lets ManualRunService's caller see the run
    /// immediately (GET /api/runs/:id, run status), before Playwright has
    /// even launched.
    /// </summary>
    Task CreatePlaceholderRunAsync(string runId, TriggeredBy triggeredBy, string environment, string browser, CancellationToken cancellationToken = default);
}
