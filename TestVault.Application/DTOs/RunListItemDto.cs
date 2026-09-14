using TestVault.Domain.Entities;
using TestVault.Domain.Enums;

namespace TestVault.Application.DTOs;

/// <summary>
/// One row of the run list. Matches the exact shape of GET /api/runs's
/// res.json(result.recordset) in backend/routes/runs.js:70-81 - i.e. the
/// bare test_runs columns, nothing nested.
///
/// Exists as its own DTO (rather than returning <see cref="TestRun"/>
/// directly, as RunsService.GetRunListAsync originally did in Phase 3)
/// specifically to leave out <see cref="TestRun.TestCases"/>: that
/// navigation property isn't populated by this query, but serializing the
/// entity as-is would still emit an unwanted "test_cases": [] on every run,
/// silently expanding the API contract this phase must preserve exactly.
/// </summary>
public class RunListItemDto
{
    public string Id { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Passed { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
    public TriggeredBy TriggeredBy { get; set; }
    public string Environment { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public RunStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public long? DurationMs { get; set; }

    public static RunListItemDto FromEntity(TestRun run) => new()
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
        DurationMs = run.DurationMs
    };
}
