using TestVault.Domain.Enums;

namespace TestVault.Domain.Entities;

/// <summary>
/// One individual Playwright test result within a <see cref="TestRun"/>.
/// Maps to the test_cases table.
/// </summary>
public class TestCase
{
    /// <summary>Database identity (int, auto-generated) - the app never assigns this itself.</summary>
    public int Id { get; set; }

    /// <summary>Foreign key to <see cref="TestRun.Id"/>.</summary>
    public string RunId { get; set; } = string.Empty;

    /// <summary>Full test title, e.g. "Login Tests &gt; User can login with valid credentials".</summary>
    public string Title { get; set; } = string.Empty;

    public TestCaseStatus Status { get; set; }

    public int DurationMs { get; set; }

    /// <summary>
    /// Nullable: the frontend renders this defensively (falls back to a
    /// placeholder when absent), indicating it isn't guaranteed to be set
    /// for every row.
    /// </summary>
    public string? Browser { get; set; }

    public int WorkerIndex { get; set; }
    public int RetryCount { get; set; }

    /// <summary>Truncated to 2000 characters by the app before it's ever persisted.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Path to a Playwright screenshot attachment. Only present on failure.</summary>
    public string? Screenshot { get; set; }

    /// <summary>Path to a Playwright video attachment. Only present on failure.</summary>
    public string? Video { get; set; }

    /// <summary>Path to a Playwright trace attachment. Only present on failure.</summary>
    public string? Trace { get; set; }

    /// <summary>Audit field: when this test case row was recorded. Always set (DB default).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Owning run. Not always populated - only when explicitly loaded together.</summary>
    public TestRun? TestRun { get; set; }

    /// <summary>AI failure analysis for this test case, if one has been run. One-to-one, optional.</summary>
    public AiAnalysis? AiAnalysis { get; set; }
}
