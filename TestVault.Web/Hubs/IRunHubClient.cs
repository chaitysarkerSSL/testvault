using TestVault.Application.Events;

namespace TestVault.Web.Hubs;

/// <summary>
/// Strongly-typed client contract for <see cref="RunHub"/> - the methods a
/// connected browser can be called back on. Direct replacement for the
/// Socket.IO events emitted by backend/routes/manual.js:
///
///   run:started  -> RunStarted
///   run:log      -> RunLog
///   run:finished -> RunFinished
///   run:stopped  -> RunStopped
///   run:error    -> RunError
///
/// Event payload shapes live in TestVault.Application.Events (see
/// RunEvents.cs) rather than here, since ManualRunService needs to know
/// what they look like without TestVault.Application ever referencing
/// TestVault.Web - see IRunNotifier.
///
/// Phase 4 originally scaffolded this interface with three different,
/// speculative method names (TestStarted/TestCompleted/RunStatusChanged)
/// for a feature that didn't have a real caller yet. Phase 5 replaces them
/// with the actual event set above, now that ManualRunService is that
/// caller - see SignalRRunNotifier for the implementation that calls these.
/// </summary>
public interface IRunHubClient
{
    Task RunStarted(RunStartedEvent runStarted);
    Task RunLog(RunLogEvent runLog);
    Task RunFinished(RunFinishedEvent runFinished);
    Task RunStopped(RunStoppedEvent runStopped);
    Task RunError(RunErrorEvent runError);
}
