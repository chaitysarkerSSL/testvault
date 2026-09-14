using TestVault.Domain.Enums;

namespace TestVault.Application.DTOs;

/// <summary>
/// A failed test case flattened together with its parent run's start time
/// and any AI analysis - exactly the 3-table projection produced by the
/// "failed tests" query in backend/routes/tests.js (GET /api/tests/failed):
/// test_cases JOIN test_runs LEFT JOIN ai_analysis.
///
/// Modeled as a DTO rather than forced onto the TestCase entity because
/// RunStartedAt/RootCause/FixSuggestion do not belong to test_cases itself -
/// they only exist here as a query-specific read projection.
/// </summary>
public class FailedTestDto
{
    public int Id { get; set; }
    public string RunId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public TestCaseStatus Status { get; set; }
    public int DurationMs { get; set; }
    public string? Browser { get; set; }
    public int WorkerIndex { get; set; }
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Screenshot { get; set; }
    public string? Video { get; set; }
    public string? Trace { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>tr.started_at from the joined test_runs row.</summary>
    public DateTime RunStartedAt { get; set; }

    /// <summary>Null when this test case has not been AI-analyzed yet.</summary>
    public string? RootCause { get; set; }

    /// <summary>Null when this test case has not been AI-analyzed yet.</summary>
    public string? FixSuggestion { get; set; }
}
