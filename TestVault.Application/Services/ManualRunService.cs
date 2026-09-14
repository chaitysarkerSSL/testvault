using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TestVault.Application.DTOs;
using TestVault.Application.Events;
using TestVault.Application.Exceptions;
using TestVault.Application.Interfaces;
using TestVault.Domain.Enums;

namespace TestVault.Application.Services;

/// <summary>
/// Business logic for manually-triggered test runs, ported from
/// backend/routes/manual.js. Composes IRunsRepository (the placeholder
/// insert), ITestRunnerProcess (spawning/streaming Playwright),
/// IManualRunTracker/IManualRunQueue (in-memory coordination), and
/// IRunNotifier (live updates) - no SQL, no direct process/HTTP calls, no
/// controller/SignalR dependency.
///
/// Split into two halves matching the two different callers:
///   - StartRunAsync/StopRunAsync/GetStatus are called by ManualController,
///     within the HTTP request's own DI scope, and must return fast.
///   - ExecuteRunAsync is called by TestVault.Web's ManualRunBackgroundService
///     from its OWN fresh DI scope (see IManualRunQueue's doc comment for
///     why), and is the one that actually blocks for as long as the test
///     suite takes to run.
/// </summary>
public class ManualRunService
{
    private readonly IRunsRepository _runsRepository;
    private readonly ITestRunnerProcess _testRunnerProcess;
    private readonly IManualRunTracker _tracker;
    private readonly IManualRunQueue _queue;
    private readonly IRunNotifier _runNotifier;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ManualRunService> _logger;

    public ManualRunService(
        IRunsRepository runsRepository,
        ITestRunnerProcess testRunnerProcess,
        IManualRunTracker tracker,
        IManualRunQueue queue,
        IRunNotifier runNotifier,
        IConfiguration configuration,
        ILogger<ManualRunService> logger)
    {
        _runsRepository = runsRepository;
        _testRunnerProcess = testRunnerProcess;
        _tracker = tracker;
        _queue = queue;
        _runNotifier = runNotifier;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Generates a run id, registers the placeholder test_runs row,
    /// announces RunStarted, and hands the actual execution off to the
    /// background queue - returning as soon as all of that quick,
    /// synchronous setup is done, well before Playwright itself runs.
    /// Ported from backend/routes/manual.js:9-32.
    /// </summary>
    /// <exception cref="ConflictException">A manual run is already active (ported from manual.js:10-14's 409).</exception>
    public async Task<string> StartRunAsync(IReadOnlyDictionary<string, string>? environmentOverrides, CancellationToken cancellationToken = default)
    {
        var runId = Guid.NewGuid().ToString();

        if (!_tracker.TryStart(runId, out var runCancellationToken))
        {
            // Message preserved verbatim from manual.js:12.
            throw new ConflictException("একটি test run ইতিমধ্যে চলছে। আগে শেষ হোক।");
        }

        var environment = environmentOverrides is not null && environmentOverrides.TryGetValue("NODE_ENV", out var nodeEnv)
            ? nodeEnv
            : "test"; // matches scripts/run-tests.js:25's own fallback

        var browser = _configuration["Playwright:Browser"] ?? "chromium";

        try
        {
            await _runsRepository.CreatePlaceholderRunAsync(runId, TriggeredBy.Manual, environment, browser, cancellationToken);
        }
        catch
        {
            // The DB write failed before anything was announced or spawned -
            // release the slot so this isn't stuck "running" forever, and
            // let the caller see the real error (-> 500 via the exception
            // middleware). Matches the original: run-tests.js's startRun()
            // is awaited before anything else happens, so a DB failure
            // there aborts the whole run before Playwright ever launches.
            _tracker.Complete(runId);
            throw;
        }

        await _runNotifier.RunStartedAsync(new RunStartedEvent(runId, DateTime.UtcNow));

        _queue.Enqueue(new ManualRunJob(runId, "manual", environmentOverrides ?? new Dictionary<string, string>(), runCancellationToken));

        return runId;
    }

    /// <summary>
    /// Signals the active run to stop and announces RunStopped immediately -
    /// mirroring manual.js:70-78, which nulled out activeProcess and emitted
    /// run:stopped synchronously, without waiting for the killed process's
    /// close event. ExecuteRunAsync deliberately does not also emit
    /// RunFinished for a run stopped this way (see its own doc comment) -
    /// the original's equivalent double-emit (both run:stopped and a
    /// success:false run:finished) was an accidental quirk, not a contract
    /// worth preserving.
    /// </summary>
    /// <exception cref="ValidationException">No run is currently active (ported from manual.js:71-73's 400).</exception>
    public async Task StopRunAsync(CancellationToken cancellationToken = default)
    {
        var runId = _tracker.CurrentRunId;

        if (!_tracker.TryStop())
        {
            // Message preserved verbatim from manual.js:72.
            throw new ValidationException("কোনো run চলছে না।");
        }

        _tracker.Complete(runId!);

        await _runNotifier.RunStoppedAsync(new RunStoppedEvent(runId!, DateTime.UtcNow));
    }

    /// <summary>Ported from GET /api/manual/status (manual.js:65-67).</summary>
    public ManualRunStatusDto GetStatus() => new()
    {
        IsRunning = _tracker.IsRunning,
        RunId = _tracker.CurrentRunId
    };

    /// <summary>
    /// Actually runs the Playwright suite and announces its outcome. Called
    /// by ManualRunBackgroundService, never by ManualController directly -
    /// this is the half of the original manual.js handler
    /// (activeProcess.stdout/stderr/close/error) that must not block an
    /// HTTP request.
    /// </summary>
    public async Task ExecuteRunAsync(ManualRunJob job, CancellationToken hostShutdownToken)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(job.CancellationToken, hostShutdownToken);

        try
        {
            var exitCode = await _testRunnerProcess.RunAsync(
                job.RunId,
                job.TriggeredBy,
                job.EnvironmentOverrides,
                (line, isError) => _ = SendLogSafeAsync(job.RunId, line, isError),
                linkedCts.Token);

            // A stop already announced RunStopped and released the tracker
            // slot itself (see StopRunAsync) - don't also announce
            // RunFinished for the same run.
            if (!job.CancellationToken.IsCancellationRequested)
            {
                var success = exitCode == 0;
                await _runNotifier.RunFinishedAsync(new RunFinishedEvent(job.RunId, success, exitCode, DateTime.UtcNow));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual run '{RunId}' failed to execute.", job.RunId);
            await _runNotifier.RunErrorAsync(new RunErrorEvent(job.RunId, ex.Message));
        }
        finally
        {
            // No-op if a stop already completed this run - see
            // IManualRunTracker.Complete's own doc comment.
            _tracker.Complete(job.RunId);
        }
    }

    private async Task SendLogSafeAsync(string runId, string line, bool isError)
    {
        try
        {
            await _runNotifier.RunLogAsync(new RunLogEvent(runId, line, isError, DateTime.UtcNow));
        }
        catch (Exception ex)
        {
            // A dropped live-log line isn't fatal to the run itself - log
            // and move on rather than let a transient SignalR failure abort
            // test execution.
            _logger.LogWarning(ex, "Failed to push a RunLog event for run '{RunId}'.", runId);
        }
    }
}
