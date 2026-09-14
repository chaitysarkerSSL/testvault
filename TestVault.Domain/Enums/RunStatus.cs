namespace TestVault.Domain.Enums;

/// <summary>
/// Lifecycle status of a <see cref="Entities.TestRun"/>.
/// Backed by test_runs.status (VARCHAR(20)).
/// Source: reporters/sql-reporter.js (onBegin inserts "running", onEnd sets
/// "passed"/"failed" based on whether any test failed) and
/// backend/routes/runs.js (dashboard queries filter WHERE status != 'running').
/// </summary>
public enum RunStatus
{
    /// <summary>Stored as "running". Set at run creation; run is in progress.</summary>
    Running,

    /// <summary>Stored as "passed". Set on completion when no tests failed.</summary>
    Passed,

    /// <summary>Stored as "failed". Set on completion when at least one test failed or timed out.</summary>
    Failed
}
