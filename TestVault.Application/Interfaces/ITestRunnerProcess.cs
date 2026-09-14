namespace TestVault.Application.Interfaces;

/// <summary>
/// Runs the actual Playwright test suite as an external process and streams
/// its console output back line by line.
///
/// This is the "test-execution bridge" established in Phase 0: Playwright
/// tests are written in Node.js and executed by Playwright's own Node test
/// runner (reporters/sql-reporter.js, still unchanged, does the real
/// per-test/per-run database writes from inside that process) - none of
/// that is being reimplemented in C#, only the *triggering* of it is
/// moving from backend/routes/manual.js to ManualRunService. Implemented in
/// TestVault.Infrastructure by spawning `npx playwright test` (see
/// ExternalServices/PlaywrightTestRunnerProcess.cs), exactly as
/// scripts/run-tests.js already does today.
/// </summary>
public interface ITestRunnerProcess
{
    /// <summary>
    /// Starts the process and returns once it exits (naturally, or because
    /// <paramref name="cancellationToken"/> was cancelled and the process
    /// was killed).
    /// </summary>
    /// <param name="runId">Passed through as TESTVAULT_RUN_ID so the reporter writes to the right test_runs row.</param>
    /// <param name="triggeredBy">Passed through as TRIGGERED_BY.</param>
    /// <param name="environmentOverrides">Additional/overriding environment variables for the child process (already validated/allow-listed by the caller).</param>
    /// <param name="onOutputLine">Invoked once per line of stdout/stderr as it's produced, with isError indicating which stream it came from.</param>
    /// <param name="cancellationToken">Cancelling this kills the process (and its full process tree).</param>
    /// <returns>The process's exit code.</returns>
    Task<int> RunAsync(
        string runId,
        string triggeredBy,
        IReadOnlyDictionary<string, string> environmentOverrides,
        Action<string, bool> onOutputLine,
        CancellationToken cancellationToken);
}
