using TestVault.Application.Interfaces;

namespace TestVault.Infrastructure.InMemory;

/// <summary>
/// In-memory implementation of <see cref="IManualRunTracker"/>. See that
/// interface for why this must be registered as a singleton (Infrastructure/DependencyInjection.cs).
/// No external I/O - lives in Infrastructure rather than Application only
/// to keep every concrete technical implementation (SQL, HTTP, OS
/// processes, and this in-memory coordination) in one place, consistent
/// with how the rest of this layer is organized.
/// </summary>
public class ManualRunTracker : IManualRunTracker
{
    private readonly object _gate = new();
    private string? _currentRunId;
    private CancellationTokenSource? _cancellationTokenSource;

    public bool IsRunning
    {
        get { lock (_gate) { return _currentRunId is not null; } }
    }

    public string? CurrentRunId
    {
        get { lock (_gate) { return _currentRunId; } }
    }

    public bool TryStart(string runId, out CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_currentRunId is not null)
            {
                cancellationToken = default;
                return false;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            _currentRunId = runId;
            cancellationToken = _cancellationTokenSource.Token;
            return true;
        }
    }

    public bool TryStop()
    {
        lock (_gate)
        {
            if (_currentRunId is null || _cancellationTokenSource is null)
            {
                return false;
            }

            _cancellationTokenSource.Cancel();
            return true;
        }
    }

    public void Complete(string runId)
    {
        lock (_gate)
        {
            if (_currentRunId != runId)
            {
                // Already released by someone else (e.g. a stop that beat
                // us here) - not this call's job to touch it.
                return;
            }

            _currentRunId = null;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }
}
