namespace TestVault.Domain.Enums;

/// <summary>
/// How a <see cref="Entities.TestRun"/> was started.
/// Backed by test_runs.triggered_by (VARCHAR(20)).
/// Source: backend/routes/manual.js sets TRIGGERED_BY='manual' when a run is
/// launched from the dashboard; scripts/run-tests.js defaults triggeredBy to
/// 'scheduler' otherwise (e.g. Windows Task Scheduler / cron, per README).
/// </summary>
public enum TriggeredBy
{
    /// <summary>Stored as "manual". Started from the dashboard's Manual Run page.</summary>
    Manual,

    /// <summary>Stored as "scheduler". Started by an external scheduler (Task Scheduler/cron), the default.</summary>
    Scheduler
}
