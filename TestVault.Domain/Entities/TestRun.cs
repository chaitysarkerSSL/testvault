using TestVault.Domain.Enums;

namespace TestVault.Domain.Entities;

/// <summary>
/// One Playwright test execution ("run"). Maps to the test_runs table.
/// Created when a run starts (status = Running) and updated once when it
/// finishes (Passed/Failed, FinishedAt and DurationMs populated).
///
/// Plain POCO - no persistence-technology attributes. Column mapping is the
/// Infrastructure layer's responsibility (Phase 2).
/// </summary>
public class TestRun
{
    /// <summary>
    /// Run id. Stored as VARCHAR(64) in the database (a GUID string produced
    /// by the Node runner's crypto.randomUUID()), not SQL Server's native
    /// uniqueidentifier - kept as string here to match the real column type
    /// exactly, since the still-active Node/Playwright reporter writes this
    /// value directly.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    public int Total { get; set; }
    public int Passed { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }

    public TriggeredBy TriggeredBy { get; set; }

    /// <summary>Free-text environment label (e.g. "test"), not an enum today - the app never constrains this to a fixed set of values.</summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>Playwright project name, e.g. "chromium".</summary>
    public string Browser { get; set; } = string.Empty;

    /// <summary>
    /// Lifecycle status of the run - "running", "passed", "failed", "aborted",
    /// etc. Backed by test_runs.status (VARCHAR(20), no CHECK constraint) and
    /// kept as a plain string rather than an enum specifically because this
    /// is a test-execution system whose set of statuses is expected to grow
    /// (e.g. "cancelled", "timeout", "retrying") - Dapper would throw
    /// ArgumentException on any DB value without a matching enum member (this
    /// is exactly what happened for "aborted"), which a string column can
    /// never do. Compare/switch on the string value; there is no fixed set
    /// of allowed statuses to enumerate here.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Audit field: when the run was created. Always set (DB default).</summary>
    public DateTime StartedAt { get; set; }

    /// <summary>Audit field: when the run completed. Null while Status is Running.</summary>
    public DateTime? FinishedAt { get; set; }

    /// <summary>Total run duration in milliseconds. Null while Status is Running.</summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// Test cases belonging to this run. Populated by the Infrastructure
    /// layer only when explicitly requested (e.g. the run-detail view) -
    /// not every query that loads a TestRun loads its test cases.
    /// </summary>
    public ICollection<TestCase> TestCases { get; set; } = new List<TestCase>();
}
