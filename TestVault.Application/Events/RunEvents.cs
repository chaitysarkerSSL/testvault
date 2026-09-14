namespace TestVault.Application.Events;

/// <summary>
/// Payloads for the manual-run live-update events, pushed to connected
/// clients via <see cref="Interfaces.IRunNotifier"/> (implemented in
/// TestVault.Web using SignalR - see Hubs/SignalRRunNotifier.cs and
/// Hubs/IRunHubClient.cs). These are the direct replacements for the
/// Socket.IO events emitted by backend/routes/manual.js:
///
///   run:started  -> RunStartedEvent
///   run:log      -> RunLogEvent
///   run:finished -> RunFinishedEvent
///   run:stopped  -> RunStoppedEvent
///   run:error    -> RunErrorEvent
///
/// Each carries the original payload's fields plus RunId, which the
/// original events never included (manual.js emitted run:started before
/// scripts/run-tests.js had even generated the id, since the two were
/// separate, uncoordinated Node processes) - now that ManualRunService
/// generates the id itself before anything is emitted, including it is a
/// straightforward, useful addition.
///
/// Defined in Application (not TestVault.Web, where the original hub
/// interface lived) so ManualRunService can depend on them without
/// TestVault.Application ever referencing TestVault.Web - see IRunNotifier.
/// </summary>
public record RunStartedEvent(string RunId, DateTime StartedAt);

public record RunLogEvent(string RunId, string Log, bool IsError, DateTime Time);

public record RunFinishedEvent(string RunId, bool Success, int ExitCode, DateTime FinishedAt);

public record RunStoppedEvent(string RunId, DateTime StoppedAt);

public record RunErrorEvent(string RunId, string Message);
