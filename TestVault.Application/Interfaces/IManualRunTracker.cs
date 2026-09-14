namespace TestVault.Application.Interfaces;

/// <summary>
/// Tracks whether a manual run is currently active. Replaces the module-level
/// `let activeProcess = null;` in backend/routes/manual.js - the single
/// piece of shared state that let /run reject a second concurrent run and
/// let /stop find the one to kill.
///
/// Must be registered as a singleton: the "start" request and a later
/// "stop" request are two different HTTP requests (two different DI
/// scopes), so this state has to live above scope, exactly like the
/// original's module-level variable lived above any single request.
/// </summary>
public interface IManualRunTracker
{
    bool IsRunning { get; }
    string? CurrentRunId { get; }

    /// <summary>
    /// Atomically claims the "a run is active" slot for <paramref name="runId"/>.
    /// Returns false (and a default token) if a run is already active.
    /// </summary>
    bool TryStart(string runId, out CancellationToken cancellationToken);

    /// <summary>
    /// Signals the active run's cancellation token, requesting it stop.
    /// Returns false if no run is currently active.
    /// </summary>
    bool TryStop();

    /// <summary>
    /// Releases the "a run is active" slot. A no-op if <paramref name="runId"/>
    /// isn't the currently tracked run (e.g. it was already released by a
    /// concurrent TryStop-triggered completion).
    /// </summary>
    void Complete(string runId);
}
