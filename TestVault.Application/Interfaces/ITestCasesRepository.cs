using TestVault.Application.DTOs;
using TestVault.Domain.Entities;

namespace TestVault.Application.Interfaces;

/// <summary>
/// Read access to test_cases. Ported from backend/routes/runs.js,
/// backend/routes/tests.js and backend/routes/analysis.js.
///
/// Read-only, for the same reason as IRunsRepository: test_cases rows are
/// written exclusively by the existing, unchanged Node.js/Playwright
/// reporter (reporters/sql-reporter.js onTestEnd).
/// </summary>
public interface ITestCasesRepository
{
    /// <summary>
    /// All test cases for one run, oldest first, each with its AI analysis
    /// populated if one exists. Ported from the test-cases half of
    /// GET /api/runs/:id (backend/routes/runs.js:94-105).
    /// </summary>
    Task<IReadOnlyList<TestCase>> GetByRunIdAsync(string runId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Most recent 20 failed test cases across all runs, each flattened
    /// with its run's start time and AI analysis (if any). Ported from
    /// GET /api/tests/failed (backend/routes/tests.js:6-25).
    /// </summary>
    Task<IReadOnlyList<FailedTestDto>> GetFailedTestsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// A single test case by id, or null if it doesn't exist. No AI
    /// analysis joined - this is a bare lookup, matching its one caller:
    /// the AI-analysis flow validating a test case before calling the
    /// Claude API. Ported from backend/routes/analysis.js:11-13.
    /// </summary>
    Task<TestCase?> GetTestDetailAsync(int testCaseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Top 10 slowest test titles by average duration. Not explicitly
    /// listed among this interface's stated responsibilities, but included
    /// because GET /api/tests/slowest (backend/routes/tests.js:28-41) is a
    /// real, existing query found while analyzing tests.js and the task
    /// asked to map every existing query - see the migration summary.
    /// </summary>
    Task<IReadOnlyList<SlowestTestDto>> GetSlowestTestsAsync(CancellationToken cancellationToken = default);
}
