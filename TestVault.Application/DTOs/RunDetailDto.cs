using TestVault.Domain.Enums;

namespace TestVault.Application.DTOs;

/// <summary>
/// A run together with its test cases. Matches the exact shape of
/// GET /api/runs/:id's res.json({ ...run, tests }) in
/// backend/routes/runs.js:111-114 - the run's fields are flattened at the
/// top level (not nested under a "run" property) with a "tests" array
/// alongside them, produced by RunsService.GetRunDetailAsync composing
/// IRunsRepository.GetRunDetailAsync + ITestCasesRepository.GetByRunIdAsync.
/// </summary>
public class RunDetailDto
{
    public string Id { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Passed { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
    public TriggeredBy TriggeredBy { get; set; }
    public string Environment { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public long? DurationMs { get; set; }

    public IReadOnlyList<TestDetailDto> Tests { get; set; } = Array.Empty<TestDetailDto>();
}
