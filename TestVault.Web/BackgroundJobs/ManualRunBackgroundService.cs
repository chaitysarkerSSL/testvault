using TestVault.Application.Interfaces;
using TestVault.Application.Services;

namespace TestVault.Web.BackgroundJobs;

/// <summary>
/// Dequeues manual-run jobs enqueued by ManualRunService.StartRunAsync and
/// actually executes them, off the HTTP request thread. This is the
/// "Do not block HTTP request while Playwright runs" half of the flow:
///
///   HTTP request -> ManualRunService.StartRunAsync (fast: id + DB
///   placeholder + RunStarted) -> IManualRunQueue.Enqueue -> returns runId
///   immediately
///                                        |
///                                        v
///   ManualRunBackgroundService (this class, running for the app's whole
///   lifetime) dequeues the job and calls ManualRunService.ExecuteRunAsync,
///   which spawns Playwright, streams RunLog events, and finally announces
///   RunFinished/RunError.
///
/// Registered as a hosted service (AddHostedService, Program.cs) rather
/// than the queuing/dequeuing living in TestVault.Application: creating a
/// new DI scope per job (IServiceScopeFactory.CreateScope()) is what gives
/// ExecuteRunAsync its own fresh scoped ManualRunService/repositories,
/// completely decoupled from the original HTTP request's scope (which has
/// long since ended by the time a real test run finishes) - the standard
/// ASP.NET Core pattern for queuing background work from a request, and
/// squarely a hosting concern belonging in TestVault.Web.
///
/// Jobs are processed one at a time deliberately: IManualRunTracker only
/// ever allows one active manual run, so there both never is a backlog to
/// parallelize in practice, and only one worker means "a run is executing"
/// exactly matches "the loop is inside ExecuteRunAsync".
/// </summary>
public class ManualRunBackgroundService : BackgroundService
{
    private readonly IManualRunQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ManualRunBackgroundService> _logger;

    public ManualRunBackgroundService(IManualRunQueue queue, IServiceScopeFactory scopeFactory, ILogger<ManualRunBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var manualRunService = scope.ServiceProvider.GetRequiredService<ManualRunService>();
                await manualRunService.ExecuteRunAsync(job, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                // ManualRunService.ExecuteRunAsync already catches and
                // reports its own failures via RunError - this is a
                // last-resort net so one bad job can never take the whole
                // background loop down.
                _logger.LogError(ex, "Unhandled failure while executing manual run '{RunId}'.", job.RunId);
            }
        }
    }
}
