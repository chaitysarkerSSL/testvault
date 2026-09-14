using TestVault.Domain.Entities;
using TestVault.Domain.Enums;

namespace TestVault.Application.DTOs;

/// <summary>
/// A test case as returned by the API. RootCause/FixSuggestion are
/// flattened directly onto this object (not nested under an "AiAnalysis"
/// property) to match the exact shape the existing React frontend expects
/// (t.root_cause / t.fix_suggestion) - see the run-detail query in
/// backend/routes/runs.js:97-105, which LEFT JOINs ai_analysis and selects
/// aa.root_cause/aa.fix_suggestion alongside tc.*.
///
/// Used both as the item type in <see cref="RunDetailDto.Tests"/> and as
/// TestsService's own "get tests by run" / "test details" return type.
/// </summary>
public class TestDetailDto
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

    /// <summary>Null when this test case has not been AI-analyzed yet.</summary>
    public string? RootCause { get; set; }

    /// <summary>Null when this test case has not been AI-analyzed yet.</summary>
    public string? FixSuggestion { get; set; }

    /// <summary>
    /// Maps a <see cref="TestCase"/> entity (with its AiAnalysis navigation
    /// populated or null) onto this DTO. Shared by RunsService and
    /// TestsService so the mapping is defined exactly once.
    /// </summary>
    public static TestDetailDto FromEntity(TestCase testCase) => new()
    {
        Id = testCase.Id,
        RunId = testCase.RunId,
        Title = testCase.Title,
        Status = testCase.Status,
        DurationMs = testCase.DurationMs,
        Browser = testCase.Browser,
        WorkerIndex = testCase.WorkerIndex,
        RetryCount = testCase.RetryCount,
        ErrorMessage = testCase.ErrorMessage,
        Screenshot = testCase.Screenshot,
        Video = testCase.Video,
        Trace = testCase.Trace,
        CreatedAt = testCase.CreatedAt,
        RootCause = testCase.AiAnalysis?.RootCause,
        FixSuggestion = testCase.AiAnalysis?.FixSuggestion
    };
}
