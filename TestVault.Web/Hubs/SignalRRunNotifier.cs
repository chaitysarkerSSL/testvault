using Microsoft.AspNetCore.SignalR;
using TestVault.Application.Events;
using TestVault.Application.Interfaces;

namespace TestVault.Web.Hubs;

/// <summary>
/// SignalR-backed implementation of <see cref="IRunNotifier"/>. Thin
/// forwarding to all connected clients via <see cref="IHubContext{THub,T}"/> -
/// there's no per-connection/group targeting today, matching the original
/// Socket.IO usage (io.emit(...) always broadcasts to every connected
/// client; backend/routes/manual.js never used rooms).
/// </summary>
public class SignalRRunNotifier : IRunNotifier
{
    private readonly IHubContext<RunHub, IRunHubClient> _hubContext;

    public SignalRRunNotifier(IHubContext<RunHub, IRunHubClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task RunStartedAsync(RunStartedEvent runStarted) => _hubContext.Clients.All.RunStarted(runStarted);

    public Task RunLogAsync(RunLogEvent runLog) => _hubContext.Clients.All.RunLog(runLog);

    public Task RunFinishedAsync(RunFinishedEvent runFinished) => _hubContext.Clients.All.RunFinished(runFinished);

    public Task RunStoppedAsync(RunStoppedEvent runStopped) => _hubContext.Clients.All.RunStopped(runStopped);

    public Task RunErrorAsync(RunErrorEvent runError) => _hubContext.Clients.All.RunError(runError);
}
