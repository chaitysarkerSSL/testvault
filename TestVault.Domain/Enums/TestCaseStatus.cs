namespace TestVault.Domain.Enums;

/// <summary>
/// Result status of a single <see cref="Entities.TestCase"/>, as reported by
/// Playwright. Backed by test_cases.status (VARCHAR(20)).
/// Source: reporters/sql-reporter.js (result.status is written as-is; the
/// onEnd summary query explicitly branches on 'passed'/'failed'/'skipped'/
/// 'timedOut').
///
/// IMPORTANT: the stored value for <see cref="TimedOut"/> is the exact
/// camelCase string "timedOut" (inherited from Playwright's own TestStatus
/// literal), not "TimedOut" or "timed_out". Any future string<->enum mapping
/// (Dapper/EF) in the Infrastructure layer must preserve this exact casing.
/// </summary>
public enum TestCaseStatus
{
    /// <summary>Stored as "passed".</summary>
    Passed,

    /// <summary>Stored as "failed".</summary>
    Failed,

    /// <summary>Stored as "skipped".</summary>
    Skipped,

    /// <summary>Stored as "timedOut" (camelCase - see remarks above).</summary>
    TimedOut
}
