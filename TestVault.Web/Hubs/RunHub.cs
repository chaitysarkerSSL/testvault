using Microsoft.AspNetCore.SignalR;

namespace TestVault.Web.Hubs;

/// <summary>
/// SignalR hub for live run updates, mapped at /hubs/run (see Program.cs).
/// Replaces the Socket.IO server in backend/server.js, which mounted a
/// single global io instance with no hub-style routing.
///
/// Deliberately thin, matching the Node original: the Node server's own
/// io.on('connection', ...) handler only logged connect/disconnect and did
/// no per-client work - all real event emission happened from route
/// handlers via req.app.get('io').emit(...), which in the SignalR model
/// corresponds to ManualRunService depending on IRunNotifier
/// (implemented by SignalRRunNotifier, which wraps
/// IHubContext&lt;RunHub, IRunHubClient&gt;) rather than the Hub class
/// itself. See IRunHubClient for the client-callable contract.
/// </summary>
public class RunHub : Hub<IRunHubClient>
{
    private readonly ILogger<RunHub> _logger;

    public RunHub(ILogger<RunHub> logger)
    {
        _logger = logger;
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("[RunHub] Client connected: {ConnectionId}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("[RunHub] Client disconnected: {ConnectionId}", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
