namespace TestVault.Application.Interfaces;

/// <summary>
/// Hands a manual run's actual execution off to background processing, so
/// the HTTP request that started it can return immediately instead of
/// blocking for however long the Playwright suite takes to run. Enqueued by
/// ManualRunService.StartRunAsync, dequeued by TestVault.Web's
/// ManualRunBackgroundService (a BackgroundService/IHostedService) - see
/// that class for why the dequeue side has to live in the Web project.
///
/// Registered as a singleton - the same instance must be written to by
/// (potentially different) request-scoped callers and read from by the one
/// long-lived hosted service.
/// </summary>
public interface IManualRunQueue
{
    void Enqueue(ManualRunJob job);

    IAsyncEnumerable<ManualRunJob> DequeueAllAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Everything ManualRunBackgroundService needs to actually execute a queued
/// manual run, once it has its own fresh DI scope (and therefore its own
/// fresh, scoped ManualRunService/repositories - see the Complete() doc
/// comment on IManualRunTracker for why that separation matters).
/// </summary>
public record ManualRunJob(
    string RunId,
    string TriggeredBy,
    IReadOnlyDictionary<string, string> EnvironmentOverrides,
    CancellationToken CancellationToken);
