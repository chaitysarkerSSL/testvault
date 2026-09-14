using TestVault.Application.DTOs;
using TestVault.Application.Exceptions;
using TestVault.Application.Interfaces;

namespace TestVault.Application.Services;

/// <summary>
/// Business logic for individual test cases, ported from
/// backend/routes/runs.js and backend/routes/tests.js. Composes
/// ITestCasesRepository only - no SQL, no HTTP, no controller/framework
/// dependency.
/// </summary>
public class TestsService
{
    private readonly ITestCasesRepository _testCasesRepository;

    public TestsService(ITestCasesRepository testCasesRepository)
    {
        _testCasesRepository = testCasesRepository;
    }

    /// <summary>
    /// All test cases for one run. Same underlying repository call as
    /// RunsService.GetRunDetailAsync uses for its "tests" array
    /// (backend/routes/runs.js:94-105) - exposed here as its own entry
    /// point for callers that only need the test list, not the run itself.
    /// </summary>
    public async Task<IReadOnlyList<TestDetailDto>> GetTestsByRunIdAsync(string runId, CancellationToken cancellationToken = default)
    {
        var tests = await _testCasesRepository.GetByRunIdAsync(runId, cancellationToken);
        return tests.Select(TestDetailDto.FromEntity).ToList();
    }

    /// <summary>
    /// Most recent 20 failed tests. Ported from GET /api/tests/failed
    /// (backend/routes/tests.js:6-25). No business rules to apply.
    /// </summary>
    public Task<IReadOnlyList<FailedTestDto>> GetFailedTestsAsync(CancellationToken cancellationToken = default)
        => _testCasesRepository.GetFailedTestsAsync(cancellationToken);

    /// <summary>
    /// Top 10 slowest test titles by average duration. Ported from
    /// GET /api/tests/slowest (backend/routes/tests.js:28-41).
    /// </summary>
    public Task<IReadOnlyList<SlowestTestDto>> GetSlowestTestsAsync(CancellationToken cancellationToken = default)
        => _testCasesRepository.GetSlowestTestsAsync(cancellationToken);

    /// <summary>
    /// A single test case by id. Ported from the lookup half of
    /// backend/routes/analysis.js:11-16 (the only place the Node app looks
    /// up one bare test case). No AI analysis is joined here - see
    /// ITestCasesRepository.GetTestDetailAsync - so RootCause/FixSuggestion
    /// are always null on the returned DTO.
    /// </summary>
    /// <exception cref="NotFoundException">No test case exists with this id (ported from analysis.js:16's 404).</exception>
    public async Task<TestDetailDto> GetTestDetailAsync(int testCaseId, CancellationToken cancellationToken = default)
    {
        var testCase = await _testCasesRepository.GetTestDetailAsync(testCaseId, cancellationToken);
        if (testCase is null)
        {
            throw new NotFoundException($"Test case '{testCaseId}' was not found.");
        }

        return TestDetailDto.FromEntity(testCase);
    }
}
