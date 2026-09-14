using TestVault.Application.Events;

namespace TestVault.Application.Interfaces;

/// <summary>
/// Pushes manual-run live-update events to connected clients. Abstracts
/// away the transport (SignalR in TestVault.Web) so ManualRunService stays
/// framework-agnostic - see Events/RunEvents.cs for what each event
/// replaces from the original Socket.IO implementation.
/// </summary>
public interface IRunNotifier
{
    Task RunStartedAsync(RunStartedEvent runStarted);
    Task RunLogAsync(RunLogEvent runLog);
    Task RunFinishedAsync(RunFinishedEvent runFinished);
    Task RunStoppedAsync(RunStoppedEvent runStopped);
    Task RunErrorAsync(RunErrorEvent runError);
}
